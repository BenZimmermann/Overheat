using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Hauptskript der prozeduralen Spinnen-Animation.
/// Auf den Körper der Spinne legen.
///
/// BLOCKING-REGELN (wer darf NICHT gleichzeitig schreiten):
///
///   FL blockiert: FR, BL   → FL darf nur schreiten wenn FR und BL am Boden sind
///   FR blockiert: FL, BR   → FR darf nur schreiten wenn FL und BR am Boden sind
///   BL blockiert: FL, BR   → BL darf nur schreiten wenn FL und BR am Boden sind
///   BR blockiert: FR, BL   → BR darf nur schreiten wenn FR und BL am Boden sind
///
///   Erlaubt:   FL+BR gleichzeitig  ✓   FR+BL gleichzeitig  ✓
///   Verboten:  FL+FR               ✗   BL+BR               ✗   FL+BL  ✗   FR+BR  ✗
///
///   Das sind genau die diagonalen Überkreuz-Paare einer Spinne.
/// </summary>
public class SpiderController : MonoBehaviour
{
    // ── Beine ─────────────────────────────────────────────────────────────────
    [Header("Beine (IK-Targets)")]
    public SpiderLeg legFrontLeft;
    public SpiderLeg legFrontRight;
    public SpiderLeg legBackLeft;
    public SpiderLeg legBackRight;

    // ── Körper ────────────────────────────────────────────────────────────────
    [Header("Körper")]
    [Tooltip("Mesh-Transform des Körpers (für Bob und Tilt)")]
    public Transform bodyMesh;

    public float bodyBobAmount = 0.05f;
    public float bodyBobSpeed = 5f;
    public float bodyRollAmount = 3f;
    public float bodyPitchAmount = 4f;
    public float bodyTiltSmoothing = 5f;

    [Header("Boden-Ausrichtung")]
    public bool alignBodyToGround = true;
    public float groundAlignSpeed = 5f;
    public float bodyHeightOffset = 0.4f;

    // ── Privat ────────────────────────────────────────────────────────────────
    private List<SpiderLeg> _allLegs;
    private Vector3 _velocity;
    private Vector3 _lastPosition;
    private float _bobTime;
    private float _currentRoll;
    private float _currentPitch;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        _allLegs = new List<SpiderLeg>
            { legFrontLeft, legFrontRight, legBackLeft, legBackRight };

        // bodyTransform zuerst setzen
        foreach (var leg in _allLegs)
            if (leg != null) leg.bodyTransform = transform;

        // ── Blocking-Regeln definieren ────────────────────────────────────────
        //
        //  Erlaubt gleichzeitig:  FL+BR  und  FR+BL
        //  → jedes Bein blockiert alle ausser seinem diagonalen Partner
        //
        SetBlocking(legFrontLeft, legFrontRight, legBackLeft);   // FL wartet auf FR und BL
        SetBlocking(legFrontRight, legFrontLeft, legBackRight);  // FR wartet auf FL und BR
        SetBlocking(legBackLeft, legBackRight, legFrontLeft);  // BL wartet auf BR und FL
        SetBlocking(legBackRight, legBackLeft, legFrontRight); // BR wartet auf BL und FR

        // Initialisieren nachdem alles gesetzt ist
        foreach (var leg in _allLegs)
            leg?.Initialize();

        _lastPosition = transform.position;
    }

    void LateUpdate()
    {
        _velocity = (transform.position - _lastPosition) / Time.deltaTime;
        _lastPosition = transform.position;

        foreach (var leg in _allLegs)
            if (leg != null) leg.bodyVelocity = _velocity;

        float speed = _velocity.magnitude;
        UpdateBodyHeight();
        UpdateBodyBob(speed);
        UpdateBodyTilt(speed);

        if (alignBodyToGround)
            AlignBodyToGround();
    }

    // ── Hilfsmethode: Blocking-Liste befüllen ──────────────────────────────────
    static void SetBlocking(SpiderLeg leg, params SpiderLeg[] blockers)
    {
        if (leg == null) return;
        leg.blockingLegs.Clear();
        foreach (var b in blockers)
            if (b != null) leg.blockingLegs.Add(b);
    }

    // ── Körper-Höhe ───────────────────────────────────────────────────────────
    void UpdateBodyHeight()
    {
        if (bodyMesh == null) return;

        float avgY = 0f;
        int n = 0;
        foreach (var leg in _allLegs)
            if (leg != null) { avgY += leg.CurrentIKPos.y; n++; }
        if (n == 0) return;

        avgY /= n;
        Vector3 p = bodyMesh.localPosition;
        p.y = Mathf.Lerp(p.y, avgY - transform.position.y + bodyHeightOffset,
                          Time.deltaTime * groundAlignSpeed);
        bodyMesh.localPosition = p;
    }

    // ── Körper-Bob ────────────────────────────────────────────────────────────
    void UpdateBodyBob(float speed)
    {
        if (bodyMesh == null) return;
        if (speed > 0.05f) _bobTime += Time.deltaTime * bodyBobSpeed * speed;

        Vector3 p = bodyMesh.localPosition;
        p.y += Mathf.Sin(_bobTime) * bodyBobAmount * Mathf.Clamp01(speed);
        bodyMesh.localPosition = p;
    }

    // ── Körper-Neigung ────────────────────────────────────────────────────────
    void UpdateBodyTilt(float speed)
    {
        if (bodyMesh == null) return;

        Vector3 lv = transform.InverseTransformDirection(_velocity);
        _currentRoll = Mathf.Lerp(_currentRoll, -lv.x * bodyRollAmount, Time.deltaTime * bodyTiltSmoothing);
        _currentPitch = Mathf.Lerp(_currentPitch, lv.z * bodyPitchAmount, Time.deltaTime * bodyTiltSmoothing);

        bodyMesh.localRotation = Quaternion.Lerp(
            bodyMesh.localRotation,
            Quaternion.Euler(_currentPitch, 0f, _currentRoll),
            Time.deltaTime * bodyTiltSmoothing);
    }

    // ── Boden-Ausrichtung ─────────────────────────────────────────────────────
    void AlignBodyToGround()
    {
        if (legFrontLeft == null || legFrontRight == null ||
            legBackLeft == null || legBackRight == null) return;

        Vector3 fl = legFrontLeft.CurrentIKPos, fr = legFrontRight.CurrentIKPos;
        Vector3 bl = legBackLeft.CurrentIKPos, br = legBackRight.CurrentIKPos;

        Vector3 n1 = Vector3.Cross(fr - fl, bl - fl).normalized;
        Vector3 n2 = Vector3.Cross(bl - fr, br - fr).normalized;
        Vector3 normal = ((n1 + n2) * 0.5f).normalized;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.FromToRotation(transform.up, normal) * transform.rotation,
            Time.deltaTime * groundAlignSpeed);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || _allLegs == null) return;

        // Erlaubte Paare in gleicher Farbe: FL+BR = Cyan, FR+BL = Gelb
        DrawPairLine(legFrontLeft, legBackRight, Color.cyan);
        DrawPairLine(legFrontRight, legBackLeft, Color.yellow);
    }

    void DrawPairLine(SpiderLeg a, SpiderLeg b, Color c)
    {
        if (a == null || b == null) return;
        Gizmos.color = c;
        Gizmos.DrawLine(a.CurrentIKPos, b.CurrentIKPos);
    }
}