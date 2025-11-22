using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BusUnjam
{
    public static class Utilities
    {
        public static bool IsCellTypeOccupied(eCellType type)
        {
            return type is
                eCellType.None 
                or eCellType.Tunnel;
        }
        
        // X offset is based on column index relative to center, Z offset moves negative for each row down
        public static Vector3 GridToWorldXZNeg(
            int cols,
            int desiredRow,
            int desiredColumn,
            float desiredDistance,
            Vector3 additionTranslate
        )
        {
            float half = (cols - 1) / 2f;
            return additionTranslate + new Vector3((desiredColumn - half) * desiredDistance, 0f, -desiredRow * desiredDistance);
        }
        
        public static bool IsInBounds(int rows, int columns, int r, int c)
            => r >= 0 && r < rows && c >= 0 && c < columns;
        
        public static List<string> GetLoadedSceneNames()
        {
            List<string> scenes = new();
            int count = SceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    scenes.Add(scene.name);
                }
            }
            return scenes;
        }

        public static bool IsSceneLoaded(string sceneName)
        {
            int count = SceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name == sceneName)
                {
                    return true;
                }
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