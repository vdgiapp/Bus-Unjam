using UnityEngine;

namespace VehicleUnjam
{
    public static class Constants
    {
        // Shader
        public static readonly int SHADER_COLOR_ID = Shader.PropertyToID("_Color"); // Built-in
        public static readonly int SHADER_BASE_COLOR_ID = Shader.PropertyToID("_BaseColor"); // URP
        
        // Animator
        public static readonly int ANIMATOR_IS_SITTING_ID = Animator.StringToHash("isSitting");
        public static readonly int ANIMATOR_IS_RUNNING_ID = Animator.StringToHash("isRunning");
        
        // Cell
        public const float CELL_DISTANCE = 0.6f;
        
        // Passenger
        public const float PASSENGER_SHAKE_STRENGTH = 15f;
        public const float PASSENGER_SHAKE_DURATION = 0.4f;
        public const int PASSENGER_SHAKE_VIBRATO = 3;
        public const float PASSENGER_ROTATE_DURATION = 0.2f;
        public const float PASSENGER_MOVE_SPEED = 2f;

        // Vehicle
        public const int VEHICLE_SEAT_SLOTS = 3;
        public const float VEHICLE_MOVE_DURATION = 2.0f;
        public const float VEHICLE_DISTANCE = 4.3f;
        public const int VEHICLE_POOL_SIZE = 5;
        
        // Raycast
        public const float MAX_RAYCAST_DISTANCE = 1000f;
        
        // Tag
        public const string TAG_NAME_MOVING = "Moving";
        public const string TAG_NAME_WAITING = "Waiting";
        public const string TAG_NAME_SITTING = "Sitting";
        
        // Layer
        public const string LAYER_NAME_PASSENGER = "Passenger";
        public const string LAYER_NAME_CELL = "Cell";
        public const string LAYER_NAME_WAITING_TILE = "WaitingTile";
    }
}