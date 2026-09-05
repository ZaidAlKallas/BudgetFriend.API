namespace BudgetFriend.API.Features.Authentication.Google;

internal sealed class GoogleAuthService(AppDbContext dbContext, IGoogleIdTokenValidator tokenValidator) : IGoogleAuthService
{
    public async Task<GoogleAuthResult> AuthenticateAsync(string idToken, CancellationToken cancellationToken)
    {
        var info = await tokenValidator.ValidateAsync(idToken, cancellationToken);

        if (string.IsNullOrWhiteSpace(info.Email))
        {
            return new GoogleAuthResult(false, null, "The Google account has no email address.");
        }

        var normalizedEmail = info.Email.ToUpperInvariant();

        var user = await dbContext.Users
            .AsTracking()
            .FirstOrDefaultAsync(u => u.GoogleSubject == info.Subject, cancellationToken);

        if (user is not null)
        {
            return new GoogleAuthResult(true, user, null);
        }

        user = await dbContext.Users
            .AsTracking()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is not null)
        {
            if (!info.EmailVerified)
            {
                return new GoogleAuthResult(
                    false,
                    null,
                    "A local account already exists for this email. Sign in with your password and connect Google from your profile.");
            }

            user.GoogleSubject = info.Subject;

            if (!user.IsEmailVerified)
            {
                user.IsEmailVerified = true;
                user.EmailVerifiedAtUtc = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return new GoogleAuthResult(true, user, null);
        }

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = info.Email,
            NormalizedEmail = normalizedEmail,
            IsEmailVerified = info.EmailVerified,
            EmailVerifiedAtUtc = info.EmailVerified ? DateTime.UtcNow : null,
            GoogleSubject = info.Subject,
            FirstName = info.FirstName,
            LastName = info.LastName,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Users.Add(newUser);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var existing = await dbContext.Users
                .AsTracking()
                .FirstOrDefaultAsync(u => u.GoogleSubject == info.Subject, cancellationToken);

            return existing is null
                ? new GoogleAuthResult(false, null, "Unable to sign in with Google. Please try again.")
                : new GoogleAuthResult(true, existing, null);
        }

        return new GoogleAuthResult(true, newUser, null);
    }
}