using System;
using DG.Tweening;
using UnityEngine;

namespace VehicleUnjam
{
    public class CloakPassenger : Passenger
    {
        // View
        [SerializeField] protected GameObject _cloakObject;
        [SerializeField] protected float _cloakOffScaleY = 0.1f;
        [SerializeField] protected float _cloakOnScaleY = 1f;
        [SerializeField] protected float _tweenDuration = 0.2f;
        
        protected Tween _cloakOffTween;
        protected Tween _cloakOnTween;

        public Tween CloakOff()
        {
            if (_cloakObject == null) return null;
            _cloakOffTween?.Kill();
            _cloakOffTween = _cloakObject.transform.DOScaleY(_cloakOffScaleY, _tweenDuration);
            return _cloakOffTween;
        }

        public Tween CloakOn()
        {
            if (_cloakObject == null) return null;
            _cloakOnTween?.Kill();
            _cloakOnTween = _cloakObject.transform.DOScaleY(_cloakOnScaleY, _tweenDuration);
            return _cloakOnTween;
        }

        public void SetCloakImmediately(bool isOn)
        {
            _cloakObject.transform.localScale = new Vector3(1f, isOn ? _cloakOnScaleY : _cloakOffScaleY, 1f);
        }
        
        public void ToggleCloak(bool toggle)
        {
            _cloakObject.SetActive(toggle);
        }

        // For undo booster
        public void PickupCloak()
        {
            _cloakObject.transform.parent = transform;
            _cloakObject.name = "Cloak";
        }
        
        public void DropCloak()
        {
            _cloakObject.transform.parent = null;
            _cloakObject.name = "Dropped Cloak";
        }
    }
}