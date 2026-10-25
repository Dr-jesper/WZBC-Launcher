using System.Net;
using System.Net.Http;

namespace PCL.Core.Minecraft.Profile.Authentication;

public enum MicrosoftAuthenticationStage
{
    XboxLive,
    Xsts,
    MinecraftLogin,
    Entitlements,
    JavaProfile
}

/// <summary>Safe HTTP context, without request credentials or remote response bodies.</summary>
public sealed class MicrosoftServiceException(MicrosoftAuthenticationStage stage, HttpStatusCode statusCode)
    : HttpRequestException($"Microsoft authentication failed at {stage}: HTTP {(int)statusCode} ({statusCode}).",
        null, statusCode)
{
    public MicrosoftAuthenticationStage Stage { get; } = stage;

    internal static void EnsureSuccess(HttpResponseMessage response, MicrosoftAuthenticationStage stage)
    {
        if (!response.IsSuccessStatusCode) throw new MicrosoftServiceException(stage, response.StatusCode);
    }
}
