using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace Player
{

    [RequireComponent(typeof(CharacterController))]

    public class PlayerController : MonoBehaviour
    {

        private CharacterController _controller;
        private Transform _animatorTransform;
        [SerializeField] private Animator _animator;

        #region Variables: Movement
            private const float _MOVE_SPEED = 5f;
            private Vector2 _move;
            private InputAction _attackAction;
            private bool _running;
        #endregion
        
        #region Variables: Inputs
            private DefaultInputActions _inputActions;
            private InputAction _moveAction;
        #endregion

        #region Variables: Animation
            private int _animRunningParamHash;
            private int _animAttackComboStepParamHash;
        #endregion

        #region Variables: Attack
            private const float _COMBO_MIN_DELAY = 0.1f;
            private const int _COMBO_MAX_STEP = 2;
            private int _comboHitStep;
            private Coroutine _comboAttackResetCoroutine;
            private bool _attacking;
        #endregion

        void Awake()
        {
            _inputActions = new DefaultInputActions();
            _controller = GetComponent<CharacterController>();

            _running = false;
            _animRunningParamHash = Animator.StringToHash("Running");

            _animatorTransform = _animator.transform;

            _animAttackComboStepParamHash = Animator.StringToHash("AttackComboStep");
        
            _comboHitStep = -1;
            _comboAttackResetCoroutine = null;

            _attacking = false;
        }
        
        void Update()
        {
            if (_attacking)
            return;
            _move = _moveAction.ReadValue<Vector2>();
            if (_move.sqrMagnitude > 0.01f)
            {
                if (!_running)
                {
                    _running = true;
                    _animator.SetBool(_animRunningParamHash, true);
                }

                Vector3 v = new Vector3(_move.x, 0f, _move.y);
                _animatorTransform.rotation =Quaternion.LookRotation(-v, Vector3.up);
                _controller.Move(
                    v *
                    Time.deltaTime *
                    _MOVE_SPEED);
            }
            else if (_running)
            {
                _running = false;
                _animator.SetBool(_animRunningParamHash, false);
            }
        }
        private void OnEnable()
        {
            _moveAction = _inputActions.Player.Move;
            _moveAction.Enable();

            _attackAction = _inputActions.Player.Attack;
            _attackAction.performed += _OnAttackAction;
            _attackAction.Enable();
        }

        private void OnDisable()
        {
            _moveAction.Disable();
            _attackAction.Disable();
        }

        private void _OnAttackAction(InputAction.CallbackContext obj)
        {
            _attacking = true;
            if (_comboHitStep == _COMBO_MAX_STEP)
                return;
            float t = _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            if (_comboHitStep == -1 || (t >= 0.1f && t <= 0.8f))
            {
                if (_comboAttackResetCoroutine != null)
                    StopCoroutine(_comboAttackResetCoroutine);
                _comboHitStep++;
                _animator.SetBool(_animRunningParamHash, false);
                _animator.SetInteger(
                    _animAttackComboStepParamHash, _comboHitStep);
                _comboAttackResetCoroutine = StartCoroutine(_ResettingAttackCombo());
            }
        }

        private IEnumerator _ResettingAttackCombo()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(
                _animator.GetAnimatorTransitionInfo(0).duration);
            yield return new WaitForEndOfFrame();
            yield return new WaitUntil(() =>
                _animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.95f);
            _comboHitStep = -1;
            _animator.SetInteger(
                _animAttackComboStepParamHash, _comboHitStep);
                
            _move = _moveAction.ReadValue<Vector2>();
            if (_move.sqrMagnitude > 0.01f && _running)
                _animator.SetBool(_animRunningParamHash, true);

            _attacking = false;
        }
    }

}