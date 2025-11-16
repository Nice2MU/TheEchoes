using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class PlayerFlattenAbility : MonoBehaviour
{
    [Header("Flatten Settings")]
    [Range(0.5f, 1.5f)] public float flattenedScaleX = 1.1f;
    [Range(0.2f, 1f)] public float flattenedScaleY = 0.5f;
    [Range(0f, 0.5f)] public float transitionDuration = 0.08f;

    [Header("Input")]
#if ENABLE_INPUT_SYSTEM
    public InputActionReference downAction;
#endif
    public KeyCode legacyDownKey = KeyCode.S;

    [Header("Optional")]
    public bool keepFeetOnGround = true;

    Transform _t;
    Vector3 _originalScale;

    [Header("Collider Reference")]
    public Collider2D mainCollider;

    bool _hasBox;
    bool _hasCapsule;
    BoxCollider2D _box;
    CapsuleCollider2D _capsule;

    Vector2 _boxSize0, _boxOffset0;
    Vector2 _capSize0, _capOffset0;

    bool _inFlattenArea = false;
    bool _isFlattened = false;
    float _tweenTime = 0f;

    void Awake()
    {
        _t = transform;
        _originalScale = _t.localScale;

        if (!mainCollider)
        {
            mainCollider = GetComponent<Collider2D>();
            if (!mainCollider) mainCollider = GetComponentInChildren<Collider2D>();
        }

        if (mainCollider)
        {
            _box = mainCollider as BoxCollider2D;
            _capsule = mainCollider as CapsuleCollider2D;
            _hasBox = _box != null;
            _hasCapsule = _capsule != null;

            if (_hasBox)
            {
                _boxSize0 = _box.size;
                _boxOffset0 = _box.offset;
            }
            else if (_hasCapsule)
            {
                _capSize0 = _capsule.size;
                _capOffset0 = _capsule.offset;
            }
        }
    }

    void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        if (downAction && downAction.action != null)
            downAction.action.Enable();
#endif
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (downAction && downAction.action != null)
            downAction.action.Disable();
#endif
    }

    void Update()
    {
        if (!_inFlattenArea)
        {
            if (_isFlattened) SmoothUnflatten();
            return;
        }

        bool requireHoldToFlatten = _currentAreaRequireHold;
        bool shouldFlatten = true;

        if (requireHoldToFlatten)
        {
            shouldFlatten = IsHoldingDown();
        }

        if (shouldFlatten) SmoothFlatten();
        else SmoothUnflatten();
    }

    bool IsHoldingDown()
    {
#if ENABLE_INPUT_SYSTEM
        if (downAction && downAction.action != null)
        {
            var ctrl = downAction.action.activeControl;
            if (ctrl != null)
            {
                if (ctrl.valueType == typeof(float))
                {
                    float v = downAction.action.ReadValue<float>();
                    if (v < -0.5f) return true;
                    if (v > 0.5f) return true;
                }
                else
                {
                    if (downAction.action.IsPressed()) return true;
                }
            }
        }
#endif
        if (Input.GetKey(legacyDownKey)) return true;
        if (Input.GetKey(KeyCode.DownArrow)) return true;
        return false;
    }

    void SmoothFlatten()
    {
        if (_isFlattened && _tweenTime >= transitionDuration) return;

        _isFlattened = true;
        _tweenTime = Mathf.Min(_tweenTime + Time.deltaTime, transitionDuration);
        float k = transitionDuration <= 0f ? 1f : (_tweenTime / transitionDuration);

        Vector3 target = new Vector3(_originalScale.x * flattenedScaleX, _originalScale.y * flattenedScaleY, _originalScale.z);
        _t.localScale = Vector3.Lerp(_t.localScale, target, k);

        if (_hasBox)
        {
            Vector2 targetSize = new Vector2(_boxSize0.x * flattenedScaleX, _boxSize0.y * flattenedScaleY);
            Vector2 targetOffset = _boxOffset0;

            if (keepFeetOnGround)
            {
                float dh = targetSize.y - _boxSize0.y;
                targetOffset.y += dh * 0.5f;
            }

            _box.size = Vector2.Lerp(_box.size, targetSize, k);
            _box.offset = Vector2.Lerp(_box.offset, targetOffset, k);
        }
        else if (_hasCapsule)
        {
            Vector2 targetSize = new Vector2(_capSize0.x * flattenedScaleX, _capSize0.y * flattenedScaleY);
            Vector2 targetOffset = _capOffset0;

            if (keepFeetOnGround)
            {
                float dh = targetSize.y - _capSize0.y;
                targetOffset.y += dh * 0.5f;
            }

            _capsule.size = Vector2.Lerp(_capsule.size, targetSize, k);
            _capsule.offset = Vector2.Lerp(_capsule.offset, targetOffset, k);
        }
    }

    void SmoothUnflatten()
    {
        if (!_isFlattened && _tweenTime <= 0f) return;

        _tweenTime = Mathf.Max(_tweenTime - Time.deltaTime, 0f);
        float k = transitionDuration <= 0f ? 1f : (1f - (_tweenTime / transitionDuration));

        _t.localScale = Vector3.Lerp(_t.localScale, _originalScale, k);

        if (_hasBox)
        {
            _box.size = Vector2.Lerp(_box.size, _boxSize0, k);
            _box.offset = Vector2.Lerp(_box.offset, _boxOffset0, k);
        }
        else if (_hasCapsule)
        {
            _capsule.size = Vector2.Lerp(_capsule.size, _capSize0, k);
            _capsule.offset = Vector2.Lerp(_capsule.offset, _capOffset0, k);
        }

        if (_tweenTime == 0f) _isFlattened = false;
    }

    bool _currentAreaRequireHold = true;
    int _areaStack = 0;

    void OnTriggerEnter2D(Collider2D other)
    {
        var area = other.GetComponent<FlattenArea>();
        if (!area) return;

        _areaStack++;
        _inFlattenArea = true;
        _currentAreaRequireHold = area.requireHoldToFlatten;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        var area = other.GetComponent<FlattenArea>();
        if (!area) return;
        _currentAreaRequireHold = area.requireHoldToFlatten;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var area = other.GetComponent<FlattenArea>();
        if (!area) return;

        _areaStack = Mathf.Max(0, _areaStack - 1);
        if (_areaStack == 0)
        {
            _inFlattenArea = false;
            _isFlattened = true;
        }
    }

    public void ForceUnflattenInstant()
    {
        _isFlattened = false;
        _tweenTime = 0f;
        _t.localScale = _originalScale;

        if (_hasBox)
        {
            _box.size = _boxSize0;
            _box.offset = _boxOffset0;
        }
        else if (_hasCapsule)
        {
            _capsule.size = _capSize0;
            _capsule.offset = _capOffset0;
        }
    }
}