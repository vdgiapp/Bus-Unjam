using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace BusUnjam
{
    public class Temp : MonoBehaviour
    {
        public TMP_InputField _inputField;
        public TMP_Text _logTmp;

        public void Play()
        {
            PlayAsync().Forget();
        }
        
        public async UniTask PlayAsync()
        {
            int level = int.Parse(_inputField.text);
            if (!(await GameManager.LoadLevel(level)))
            {
                _logTmp.text = $"Level {level} is not found.";
            }
        }
    }
}