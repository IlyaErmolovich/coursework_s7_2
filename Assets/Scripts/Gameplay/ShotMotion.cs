using UnityEngine;
namespace WinterDuel.Gameplay
{
    public static class ShotMotion
    {
        // Both the actual swept flight and the hint use this fixed-step integrator.
        public static void Step(ref Vector2 position,ref Vector2 velocity,float dt,float gravity,float wind)
        {
            velocity+=new Vector2(wind,-gravity)*dt;
            position+=velocity*dt;
        }
    }
}
