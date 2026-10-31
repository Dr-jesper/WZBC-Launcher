using System;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL.Core.Conduit;

public static class GameLaunchEligibility
{
    // Temporary fail-closed boundary. Account type and cached credentials are not
    // entitlement evidence. Replace this rejection only with official verification,
    // not a service-approved flag or user-configurable bypass.
    public static void EnsureCanStart(McProfile? profile)
        => throw new InvalidOperationException(
            "当前版本暂不支持启动游戏或生成启动脚本：尚无法核验官方游戏资格。账号、皮肤、实例及设置管理仍可使用。");
}
