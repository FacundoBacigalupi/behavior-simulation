using UnityEngine;
using BehaviorSimulation.Core;

namespace BehaviorSimulation.DecisionAI
{
    // Patrol → Chase → Attack → Flee, driven by an explicit state machine.
    public class FSMNPCAgent : MonoBehaviour
    {
        public enum State { Patrol, Chase, Attack, Flee }

        static readonly Color ColPatrol = new(0.35f, 0.55f, 1.00f);
        static readonly Color ColChase  = new(0.95f, 0.88f, 0.15f);
        static readonly Color ColAttack = new(0.95f, 0.30f, 0.10f);
        static readonly Color ColFlee   = new(0.80f, 0.22f, 0.88f);

        NPCSettings    _s;
        Vector2[]      _waypoints;
        int            _wpIdx;
        float          _hp;
        SpriteRenderer _sr;

        public State  CurrentState { get; private set; } = State.Patrol;
        public float  HP           => _hp;
        public bool   IsAttacking  { get; private set; }

        public void Init(NPCSettings settings, Vector2[] waypoints)
        {
            _s          = settings;
            _waypoints  = waypoints;
            _hp         = settings.maxHP;
            _sr         = GetComponent<SpriteRenderer>();
            _sr.sprite  = SpriteFactory.Circle(16, 32f);
        }

        public void Tick(float dt, Vector2 targetPos)
        {
            Vector2 pos  = transform.position;
            float   dist = Vector2.Distance(pos, targetPos);
            IsAttacking  = false;

            // ── Transitions ───────────────────────────────────────────────────
            switch (CurrentState)
            {
                case State.Patrol:
                    if (_hp < _s.maxHP * _s.fleeHpPct)   CurrentState = State.Flee;
                    else if (dist <= _s.attackRange)      CurrentState = State.Attack;
                    else if (dist <= _s.chaseRange)       CurrentState = State.Chase;
                    break;
                case State.Chase:
                    if (_hp < _s.maxHP * _s.fleeHpPct)   CurrentState = State.Flee;
                    else if (dist <= _s.attackRange)      CurrentState = State.Attack;
                    else if (dist > _s.chaseRange)        CurrentState = State.Patrol;
                    break;
                case State.Attack:
                    if (_hp < _s.maxHP * _s.fleeHpPct)   CurrentState = State.Flee;
                    else if (dist > _s.attackRange)       CurrentState = State.Chase;
                    break;
                case State.Flee:
                    if (_hp >= _s.maxHP * _s.recoverHpPct) CurrentState = State.Patrol;
                    break;
            }

            // ── Execute ───────────────────────────────────────────────────────
            Vector2 vel = Vector2.zero;

            switch (CurrentState)
            {
                case State.Patrol:
                    Vector2 wp = _waypoints[_wpIdx];
                    if (Vector2.Distance(pos, wp) < 0.4f)
                        _wpIdx = (_wpIdx + 1) % _waypoints.Length;
                    vel = (_waypoints[_wpIdx] - pos).normalized * _s.patrolSpeed;
                    _hp = Mathf.Min(_s.maxHP, _hp + _s.regenRate * dt);
                    break;

                case State.Chase:
                    vel = (targetPos - pos).normalized * _s.chaseSpeed;
                    _hp = Mathf.Min(_s.maxHP, _hp + _s.regenRate * dt);
                    break;

                case State.Attack:
                    // Slowly track target so it can't simply walk away from combat
                    vel         = dist > 0.2f
                        ? (targetPos - pos).normalized * (_s.patrolSpeed * 0.6f)
                        : Vector2.zero;
                    IsAttacking = true;
                    _hp        -= _s.attackDamage * dt;  // target fights back
                    break;

                case State.Flee:
                    vel = (pos - targetPos).normalized * _s.fleeSpeed;
                    _hp = Mathf.Min(_s.maxHP, _hp + _s.regenRate * dt * 0.5f);
                    break;
            }

            // ── Integrate ─────────────────────────────────────────────────────
            float hw = _s.boundsHalfW - 0.3f, hh = _s.boundsHalfH - 0.3f;
            pos = new Vector2(
                Mathf.Clamp(pos.x + vel.x * dt, -hw, hw),
                Mathf.Clamp(pos.y + vel.y * dt, -hh, hh));
            transform.position = new Vector3(pos.x, pos.y, 0f);

            _sr.color = CurrentState switch
            {
                State.Patrol => ColPatrol,
                State.Chase  => ColChase,
                State.Attack => ColAttack,
                State.Flee   => ColFlee,
                _            => Color.white
            };
        }

        public void ResetAgent(Vector2 startPos)
        {
            _hp           = _s.maxHP;
            _wpIdx        = 0;
            CurrentState  = State.Patrol;
            IsAttacking   = false;
            transform.position = new Vector3(startPos.x, startPos.y, 0f);
            if (_sr) _sr.color = ColPatrol;
        }
    }
}
