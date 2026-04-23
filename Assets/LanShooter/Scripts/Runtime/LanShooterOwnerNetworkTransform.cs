using Unity.Netcode.Components;

namespace LanShooter
{
    public sealed class LanShooterOwnerNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
