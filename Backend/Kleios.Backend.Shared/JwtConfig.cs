using System;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Kleios.Backend.Shared
{
    /// <summary>
    /// Classe statica che contiene le costanti di configurazione JWT per l'autenticazione
    /// </summary>
    public static class JwtConfig
    {
        /// <summary>
        /// Chiave segreta per la firma dei token JWT
        /// La chiave dovrebbe essere almeno di 32 caratteri per una sicurezza adeguata
        /// </summary>
        public const string SecretKey = "Kleios_JWT_Secret_Key_For_Development_At_Least_32_Characters";

        /// <summary>
        /// Emittente del token (chi ha generato il token)
        /// </summary>
        public const string Issuer = "Kleios";

        /// <summary>
        /// Pubblico destinatario del token
        /// </summary>
        public const string Audience = "KleiosUsers";

        /// <summary>
        /// Durata di validità del token in minuti
        /// </summary>
        public const int TokenValidityInMinutes = 60;

        /// <summary>
        /// Durata di validità del refresh token in giorni
        /// </summary>
        public const int RefreshTokenValidityInDays = 7;

        /// <summary>
        /// Restituisce la chiave di firma per i token JWT
        /// </summary>
        /// <returns>SymmetricSecurityKey basata sulla chiave segreta</returns>
        public static SymmetricSecurityKey GetSigningKey()
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        }

        /// <summary>
        /// Restituisce le credenziali di firma per i token JWT
        /// </summary>
        /// <returns>SigningCredentials con l'algoritmo HMAC SHA256</returns>
        public static SigningCredentials GetSigningCredentials()
        {
            return new SigningCredentials(GetSigningKey(), SecurityAlgorithms.HmacSha256);
        }

        /// <summary>
        /// Restituisce i parametri di validazione del token
        /// </summary>
        /// <param name="validateLifetime">Indica se validare la durata del token</param>
        /// <param name="requireExpirationTime">Indica se richiedere un tempo di scadenza</param>
        /// <param name="requireSignedTokens">Indica se richiedere che i token siano firmati</param>
        /// <returns>TokenValidationParameters configurati</returns>
        public static TokenValidationParameters GetTokenValidationParameters(
            bool validateLifetime = true,
            bool requireExpirationTime = true,
            bool requireSignedTokens = true)
        {
            return new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = validateLifetime,
                ValidateIssuerSigningKey = true,
                RequireExpirationTime = requireExpirationTime,
                RequireSignedTokens = requireSignedTokens,
                ValidIssuer = Issuer,
                ValidAudience = Audience,
                IssuerSigningKey = GetSigningKey(),
                // Aumenta il ClockSkew durante lo sviluppo per risolvere problemi di sincronizzazione dell'orario
                ClockSkew = TimeSpan.FromMinutes(5)
            };
        }
    }
}