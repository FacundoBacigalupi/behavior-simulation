using UnityEngine;
using BehaviorSimulation.Core;

namespace BehaviorSimulation.DecisionAI
{
    // Same patrol/chase/attack/flee behavior as FSMNPCAgent but driven by a Behavior Tree.
    public class BTNPCAgent : MonoBehaviour
    {
        static readonly Color ColPatrol = new(0.25f, 0.85f, 0.45f);
        static readonly Color ColChase  = new(0.95f, 0.88f, 0.15f);
        static readonly Color ColAttack = new(0.95f, 0.30f, 0.10f);
        static readonly Color ColFlee   = new(0.80f, 0.22f, 0.88f);

        NPCSettings    _s;
        Vector2[]      _waypoints;
        int            _wpIdx;
        float          _hp;
        SpriteRenderer _sr;
        BTNode         _tree;

        // Per-tick context (written before tree runs, read by leaf lambdas)
        float   _dt, _dist;
        Vector2 _pos, _targetPos;

        // Leaf nodes write output here
        Vector2 _desiredVel;
        Color   _color;
        bool    _attacking;
        string  _stateName;

        // Persistent flee flag — mirrors FSM's recoverHpPct hysteresis
        bool _isFleeing;

        public string StateName  => _stateName;
        public float  HP         => _hp;
        public bool   IsAttacking => _attacking;

        public void Init(NPCSettings settings, Vector2[] waypoints)
        {
            _s         = settings;
            _waypoints = waypoints;
            _hp        = settings.maxHP;
            _sr        = GetComponent<SpriteRenderer>();
            _sr.sprite = SpriteFactory.Circle(16, 32f);
            _tree      = BuildTree();
        }

        BTNode BuildTree() => new Selector(

            // Priority 1 — Flee when critically wounded; don't stop until HP recovers to recoverHpPct
            new Sequence(
                new Leaf(() =>
                {
                    if (_hp < _s.maxHP * _s.fleeHpPct)    _isFleeing = true;
                    if (_hp >= _s.maxHP * _s.recoverHpPct) _isFleeing = false;
                    return _isFleeing ? BTStatus.Success : BTStatus.Failure;
                }),
                new Leaf(() =>
                {
                    _stateName   = "Flee";
                    _color       = ColFlee;
                    _desiredVel  = (_pos - _targetPos).normalized * _s.fleeSpeed;
                    _hp          = Mathf.Min(_s.maxHP, _hp + _s.regenRate * _dt * 0.5f);
                    return BTStatus.Running;
                })
            ),

            // Priority 2 — Attack when within melee range
            new Sequence(
                new Leaf(() => _dist <= _s.attackRange
                    ? BTStatus.Success : BTStatus.Failure),
                new Leaf(() =>
                {
                    _stateName  = "Attack";
                    _color      = ColAttack;
                    // Slowly track target so it can't simply walk away from combat
                    _desiredVel = _dist > 0.2f
                        ? (_targetPos - _pos).normalized * (_s.patrolSpeed * 0.6f)
                        : Vector2.zero;
                    _attacking  = true;
                    _hp        -= _s.attackDamage * _dt;  // target fights back
                    return BTStatus.Running;
                })
            ),

            // Priority 3 — Chase when target is visible (in range)
            new Sequence(
                new Leaf(() => _dist <= _s.chaseRange
                    ? BTStatus.Success : BTStatus.Failure),
                new Leaf(() =>
                {
                    _stateName  = "Chase";
                    _color      = ColChase;
                    _desiredVel = (_targetPos - _pos).normalized * _s.chaseSpeed;
                    _hp         = Mathf.Min(_s.maxHP, _hp + _s.regenRate * _dt);
                    return BTStatus.Running;
                })
            ),

            // Priority 4 (fallback) — Patrol waypoints
            new Leaf(() =>
            {
                _stateName = "Patrol";
                _color     = ColPatrol;
                Vector2 wp = _waypoints[_wpIdx];
                if (Vector2.Distance(_pos, wp) < 0.4f)
                    _wpIdx = (_wpIdx + 1) % _waypoints.Length;
                _desiredVel = (_waypoints[_wpIdx] - _pos).normalized * _s.patrolSpeed;
                _hp         = Mathf.Min(_s.maxHP, _hp + _s.regenRate * _dt);
                return BTStatus.Running;
            })
        );

        public void Tick(float dt, Vector2 targetPos)
        {
            _dt        = dt;
            _pos       = transform.position;
            _targetPos = targetPos;
            _dist      = Vector2.Distance(_pos, _targetPos);
            _attacking = false;
            _desiredVel = Vector2.zero;

            _tree.Tick();

            float hw = _s.boundsHalfW - 0.3f, hh = _s.boundsHalfH - 0.3f;
            _pos = new Vector2(
                Mathf.Clamp(_pos.x + _desiredVel.x * dt, -hw, hw),
                Mathf.Clamp(_pos.y + _desiredVel.y * dt, -hh, hh));
            transform.position = new Vector3(_pos.x, _pos.y, 0f);
            _sr.color = _color;
        }

        public void ResetAgent(Vector2 startPos)
        {
            _hp        = _s.maxHP;
            _wpIdx     = 0;
            _attacking = false;
            _isFleeing = false;
            _stateName = "Patrol";
            transform.position = new Vector3(startPos.x, startPos.y, 0f);
            if (_sr) _sr.color = ColPatrol;
        }
    }
}
