using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Steuert einen einzelnen Spinnenbein-IK-Target.
/// Auf das IK-Target-GameObject jedes Beins legen.
///
/// SCHRITT-REGEL (garantiert kein Nachbar gleichzeitig in der Luft):
///   Jedes Bein kennt seine "blockingLegs" – alle Beine die NICHT gleichzeitig
///   schreiten dürfen. Ein Bein wartet, bis ALLE blockingLegs am Boden sind.
///
///   Zuweisung im SpiderController:
///     FL blockiert: FR, BL        (linke Seite + vordere Reihe)
///     FR blockiert: FL, BR        (rechte Seite + vordere Reihe)
///     BL blockiert: BR, FL        (linke Seite + hintere Reihe)
///     BR blockiert: BL, FR        (rechte Seite + hintere Reihe)
///
///   Erlaubte gleichzeitige Schritte: nur FL+BR oder FR+BL (echtes Überkreuz).
/// </summary>
public class SpiderLeg : MonoBehaviour
{
    [Header("Schritt-Einstellungen")]
    public float stepDistance = 0.4f;
    public float stepHeight = 0.25f;
    public float stepSpeed = 10f;

    [Header("Bodenerkennung")]
    public LayerMask groundLayer = ~0;
    public float raycastDistance = 2.5f;

    [Header("Offset")]
    [Tooltip("Ruheposition lokal zum Körper. X = seitlich, Z = vor/zurück.")]
    public Vector3 restPositionOffset = new Vector3(0.5f, 0f, 0f);

    [Tooltip("Vorausversatz in Laufrichtung beim Schritt.")]
    public float stepOvershoot = 0.15f;

    // ── Vom Controller gesetzt ────────────────────────────────────────────────
    [HideInInspector] public Transform bodyTransform;
    [HideInInspector] public Vector3 bodyVelocity;

    /// <summary>
    /// Alle Beine, die NICHT gleichzeitig schreiten dürfen.
    /// Solange auch nur eines davon IsStepping == true hat, wartet dieses Bein.
    /// </summary>
    [HideInInspector] public List<SpiderLeg> blockingLegs = new List<SpiderLeg>();

    // ── Zustand ───────────────────────────────────────────────────────────────
    private Vector3 _currentGroundPos;
    private Vector3 _targetGroundPos;
    private Vector3 _stepStartPos;
    private float _stepProgress;
    private bool _isStepping;
    private bool _initialized;

    public bool IsStepping => _isStepping;
    public Vector3 CurrentIKPos => transform.position;

    // ─────────────────────────────────────────────────────────────────────────

    public void Initialize()
    {
        if (_initialized) return;

        _currentGroundPos = TryGetGroundPoint(out Vector3 hit) ? hit : GetRestWorldPos();
        _targetGroundPos = _currentGroundPos;
        transform.position = _currentGroundPos;
        _initialized = true;
    }

    void Update()
    {
        if (!_initialized)
        {
            if (bodyTransform == null) return;
            Initialize();
        }

        if (_isStepping)
            AnimateStep();
        else
            CheckForStep();

        if (!_isStepping)
            transform.position = _currentGroundPos;
    }

    // ── Schritt-Check ─────────────────────────────────────────────────────────
    void CheckForStep()
    {
        // Kein Schritt solange ein blockierendes Nachbarbein in der Luft ist
        foreach (var blocker in blockingLegs)
            if (blocker != null && blocker.IsStepping) return;

        float dist = Vector3.Distance(GetRestWorldPos(), _currentGroundPos);
        if (dist < stepDistance) return;

        if (!TryGetGroundPoint(out Vector3 newPos)) return;

        _targetGroundPos = newPos;
        _stepStartPos = _currentGroundPos;
        _stepProgress = 0f;
        _isStepping = true;
    }

    // ── Schritt-Animation ─────────────────────────────────────────────────────
    void AnimateStep()
    {
        _stepProgress += Time.deltaTime * stepSpeed;
        float t = Mathf.Clamp01(_stepProgress);

        transform.position = Vector3.Lerp(_stepStartPos, _targetGroundPos, t)
                           + Vector3.up * (Mathf.Sin(t * Mathf.PI) * stepHeight);

        if (t >= 1f)
        {
            _currentGroundPos = _targetGroundPos;
            transform.position = _currentGroundPos;
            _isStepping = false;
        }
    }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────────

    Vector3 GetRestWorldPos()
    {
        if (bodyTransform == null) return transform.position;
        return bodyTransform.TransformPoint(restPositionOffset);
    }

    bool TryGetGroundPoint(out Vector3 point)
    {
        Vector3 rest = GetRestWorldPos();
        Vector3 origin = rest + bodyVelocity.normalized * stepOvershoot + Vector3.up * 1.2f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
        { point = hit.point; return true; }

        if (Physics.Raycast(rest + Vector3.up * 1.2f, Vector3.down, out RaycastHit hit2, raycastDistance, groundLayer))
        { point = hit2.point; return true; }

        point = Vector3.zero;
        return false;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    void OnDrawGizmos()
    {
        if (bodyTransform != null)
        {
            // Farbe zeigt ob das Bein schreiten darf (grün) oder blockiert ist (orange)
            bool blocked = false;
            foreach (var b in blockingLegs)
                if (b != null && b.IsStepping) { blocked = true; break; }

            Gizmos.color = _isStepping ? Color.red : (blocked ? Color.magenta : Color.green);
            Vector3 rest = GetRestWorldPos();
            Gizmos.DrawWireSphere(rest, stepDistance * 0.5f);
            Gizmos.DrawSphere(rest, 0.04f);
        }

        Gizmos.color = _isStepping ? Color.red : Color.green;
        Gizmos.DrawSphere(transform.position, 0.06f);

        if (_isStepping)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(_targetGroundPos, 0.05f);
            Gizmos.DrawLine(transform.position, _targetGroundPos);
        }
    }
}