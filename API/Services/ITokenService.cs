using LearnToCode.Data;

namespace LearnToCode.API.Services;

public interface ITokenService
{
    TokenGenerationResult CreateToken(User user, string sessionId);
}