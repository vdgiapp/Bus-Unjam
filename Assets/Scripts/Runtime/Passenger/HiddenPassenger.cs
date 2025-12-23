using DG.Tweening;
using TMPro;
using UnityEngine;

namespace VehicleUnjam
{
    public class HiddenPassenger : Passenger
    {
        [SerializeField] private Color _hiddenColor = Color.black;
        [SerializeField] private float _tweenDuration = 0.2f;
        [SerializeField] private TMP_Text _textTmp;
        
        private bool _isRevealed;
        
        private Tween _revealTween;
        private Tween _concealTween;

        public Tween Reveal(Color newColor)
        {
            _revealTween?.Kill();
            _revealTween = DOVirtual.Color(_hiddenColor, newColor, _tweenDuration, SetColor);
            _textTmp.gameObject.SetActive(false);
            return _revealTween;
        }

        // For undo booster
        public Tween Conceal(Color fromColor)
        {
            _concealTween?.Kill();
            _concealTween = DOVirtual.Color(fromColor, _hiddenColor, _tweenDuration, SetColor);
            _textTmp.gameObject.SetActive(true);
            return _concealTween;
        }
    }
}