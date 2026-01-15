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

        public const float FAILED_TIME_CHECK = 0.5f;
        
        // Cell
        public const float CELL_DISTANCE = 0.6f;
        public const string CELL_GAMEOBJECT_NAME = "Cell";
        public const string WAITING_TILE_GAMEOBJECT_NAME = "WaitingTile";
        
        // Passenger
        public const float PASSENGER_OUT_TUNNEL_DURATION = 0.75f;
        public const float PASSENGER_SHAKE_STRENGTH = 15f;
        public const float PASSENGER_SHAKE_DURATION = 0.4f;
        public const int PASSENGER_SHAKE_VIBRATO = 3;
        public const float PASSENGER_ROTATE_DURATION = 0.2f;
        public const float PASSENGER_MOVE_SPEED = 1.75f;
        public const int PASSENGER_MAX_ROPE_COUNT = 4;
        public const string PASSENGER_GAMEOBJECT_NAME = "Passenger";

        // Vehicle
        public const int VEHICLE_SEAT_SLOTS = 3;
        public const float VEHICLE_MOVE_DURATION = 2.0f;
        public const float VEHICLE_DISTANCE = 4.3f;
        public const int VEHICLE_ACTIVE_SIZE = 5;
        public const string VEHICLE_GAMEOBJECT_NAME = "Vehicle";
        
        // Raycast
        public const float MAX_RAYCAST_DISTANCE = 1000f;
        
        // Layer
        public const string LAYER_NAME_PASSENGER = "Passenger";
        public const string LAYER_NAME_CELL = "Cell";
        public const string LAYER_NAME_WAITING_TILE = "WaitingTile";
        
        // Editor
        public const string LEVEL_FOLDER_PATH = "Assets/Data/Levels";
    }
}