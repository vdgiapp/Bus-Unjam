using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VehicleUnjam
{
    public static class Utilities
    {
        /// <summary>
        /// X offset is based on column index relative to center, Z offset moves negative for each row down
        /// </summary>
        public static Vector3 GridToWorldXZNeg(int columns, int r, int c, float distance, Vector3 root)
        {
            float half = (columns - 1) / 2f;
            return root + new Vector3((c - half) * distance, 0f, -r * distance);
        }
        
        public static bool IsCellTypeIgnoreOccupied(eCellType type)
        {
            return type is
                eCellType.None;
                //or eCellType.Tunnel;
        }
        
        public static bool IsInBounds(int rows, int columns, int r, int c)
        {
            return (r >= 0) && (r < rows) && (c >= 0) && (c < columns);
        }
        
        public static List<string> GetLoadedScenesName()
        {
            List<string> scenes = new();
            int count = SceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded) scenes.Add(scene.name);
            }
            return scenes;
        }

        public static bool IsSceneLoaded(string sceneName)
        {
            int count = SceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name == sceneName) return true;
            }
            return false;
        }
    }
    
    [Serializable]
    public struct KeyValue<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;
    }
}