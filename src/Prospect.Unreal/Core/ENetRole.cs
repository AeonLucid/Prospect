namespace Prospect.Unreal.Core;

public enum ENetRole
{
    /** No role at all. */
    ROLE_None,
    /** Locally simulated proxy of this actor. */
    ROLE_SimulatedProxy,
    /** Locally autonomous proxy of this actor. */
    ROLE_AutonomousProxy,
    /** Authoritative control over the actor. */
    ROLE_Authority,
    ROLE_MAX,
}