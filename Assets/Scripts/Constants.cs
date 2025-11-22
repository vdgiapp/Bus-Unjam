using UnityEngine;

namespace BusUnjam
{
    public static class Constants
    {
        // Shader
        public static readonly int ShaderColorID = Shader.PropertyToID("_Color");
        //public static readonly int ShaderBaseColorID = Shader.PropertyToID("_BaseColor"); // URP
        
        // Animator
        public static readonly int AnimatorIsSittingID = Animator.StringToHash("isSitting");
        public static readonly int AnimatorIsRunningID = Animator.StringToHash("isRunning");
    }
}