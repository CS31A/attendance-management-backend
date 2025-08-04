using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace attendance_monitoring.Classes;

public class TokenGenerator
{
   public string GenerateToken(Guid userId, string email)
   {
      var tokenHandler = new JwtSecurityToken();
      var key = "SecureKeyLigma"u8.ToArray();

      var claims = new List<Claim>
      {
         new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
         new(JwtRegisteredClaimNames.Sub, userId.ToString()),
         new(JwtRegisteredClaimNames.Email, email)
      };
      return null;
   } 
}