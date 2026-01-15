using DG.Tweening;
using TMPro;
using UnityEngine;

namespace VehicleUnjam
{
    public class HiddenPassenger : Passenger
    {
        [SerializeField] protected Color _hiddenColor = Color.black;
        [SerializeField] protected float _tweenDuration = 0.2f;
        [SerializeField] protected TMP_Text _textTmp;
        
        protected Tween _revealTween;
        protected Tween _concealTween;
        protected bool _isRevealed;

        public Tween Reveal(Color newColor)
        {
            _revealTween?.Kill();
            _revealTween = DOVirtual.Color(_hiddenColor, newColor, _tweenDuration, SetColor);
            _textTmp.gameObject.SetActive(false);
            _isRevealed = true;
            return _revealTween;
        }

        // For undo booster
        public Tween Conceal(Color fromColor)
        {
            _concealTween?.Kill();
            _concealTween = DOVirtual.Color(fromColor, _hiddenColor, _tweenDuration, SetColor);
            _textTmp.gameObject.SetActive(true);
            _isRevealed = false;
            return _concealTween;
        }
        
        public bool IsRevealed() => _isRevealed;
        
        public void SetConcealedImmediately()
        {
            _isRevealed = false;
            _textTmp.gameObject.SetActive(true);
            SetColor(_hiddenColor);
        }
        
        public void SetRevealedImmediately()
        {
            _isRevealed = false;
            _textTmp.gameObject.SetActive(true);
            SetColor(_hiddenColor);
        }
    }
}