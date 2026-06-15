using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BehaviorSimulation.Core;

namespace BehaviorSimulation.DecisionAI
{
    public class NPCManager : MonoBehaviour, ISimulation
    {
        [SerializeField] private NPCSettings settings;

        readonly List<FSMNPCAgent> _fsm = new();
        readonly List<BTNPCAgent>  _bt  = new();

        Vector2[]      _fsmStartPos;
        Vector2[][]    _fsmWaypoints;
        Vector2[]      _btStartPos;
        Vector2[][]    _btWaypoints;

        SpriteRenderer _targetSR;
        Vector2        _targetPos;
        float          _targetAngle;

        bool _isPlaying;

        // ── State counts for UI ───────────────────────────────────────────────
        public int FSMPatrol  => _fsm.Count(a => a.CurrentState == FSMNPCAgent.State.Patrol);
        public int FSMChase   => _fsm.Count(a => a.CurrentState == FSMNPCAgent.State.Chase);
        public int FSMAttack  => _fsm.Count(a => a.CurrentState == FSMNPCAgent.State.Attack);
        public int FSMFlee    => _fsm.Count(a => a.CurrentState == FSMNPCAgent.State.Flee);
        public int BTPatrol   => _bt.Count(a => a.StateName == "Patrol");
        public int BTChase    => _bt.Count(a => a.StateName == "Chase");
        public int BTAttack   => _bt.Count(a => a.StateName == "Attack");
        public int BTFlee     => _bt.Count(a => a.StateName == "Flee");

        // ── Unity ─────────────────────────────────────────────────────────────

        void Start()
        {
            SimulationController.Instance?.Register(this);
            // Find the Target GO by name (created by the scene builder)
            var targetGO = GameObject.Find("Target");
            if (targetGO) _targetSR = targetGO.GetComponent<SpriteRenderer>();

            PrecomputeSpawns();
            SpawnAgents();
            MoveTarget(0f);
        }

        void Update()
        {
            if (_isPlaying) Tick(Time.deltaTime);
        }

        // ── ISimulation ───────────────────────────────────────────────────────

        public void Play()  => _isPlaying = true;
        public void Pause() => _isPlaying = false;
        public void Step()  { if (!_isPlaying) Tick(Time.fixedDeltaTime); }

        public void ResetSimulation()
        {
            _isPlaying   = false;
            _targetAngle = 0f;
            SpawnAgents();  // clear and re-create all agents
            MoveTarget(0f);
        }

        // ── Simulation ────────────────────────────────────────────────────────

        void Tick(float dt)
        {
            _targetAngle += settings.targetSpeed * dt;
            MoveTarget(dt);

            foreach (var a in _fsm) a.Tick(dt, _targetPos);
            foreach (var a in _bt)  a.Tick(dt, _targetPos);
        }

        void MoveTarget(float dt)
        {
            // Lissajous figure-8: x = A·sin(t),  y = B·sin(2t)
            float t = _targetAngle;
            _targetPos = new Vector2(
                settings.targetA * Mathf.Sin(t),
                settings.targetB * Mathf.Sin(2f * t));
            if (_targetSR)
                _targetSR.transform.position = new Vector3(_targetPos.x, _targetPos.y, -0.1f);
        }

        // ── Spawn ─────────────────────────────────────────────────────────────

        void PrecomputeSpawns()
        {
            int n = settings.fsmCount;
            _fsmStartPos  = new Vector2[n];
            _fsmWaypoints = new Vector2[n][];

            for (int i = 0; i < n; i++)
            {
                float y = Mathf.Lerp(-8f, 8f, n == 1 ? 0.5f : (float)i / (n - 1));
                _fsmStartPos[i]  = new Vector2(-13f, y);
                _fsmWaypoints[i] = new Vector2[]
                {
                    new(-16f, y - 3f),
                    new(-13f, y + 1f),
                    new(-9f,  y - 1f),
                    new(-13f, y),
                };
            }

            int m = settings.btCount;
            _btStartPos  = new Vector2[m];
            _btWaypoints = new Vector2[m][];

            for (int i = 0; i < m; i++)
            {
                float y = Mathf.Lerp(-8f, 8f, m == 1 ? 0.5f : (float)i / (m - 1));
                _btStartPos[i]  = new Vector2(13f, y);
                _btWaypoints[i] = new Vector2[]
                {
                    new(16f, y - 3f),
                    new(13f, y + 1f),
                    new(9f,  y - 1f),
                    new(13f, y),
                };
            }
        }

        // Called by scene builder after component is added
        public void SpawnAgents()
        {
            foreach (var a in _fsm) if (a) Destroy(a.gameObject);
            _fsm.Clear();
            foreach (var a in _bt)  if (a) Destroy(a.gameObject);
            _bt.Clear();

            for (int i = 0; i < settings.fsmCount; i++)
            {
                var go = new GameObject($"FSM_{i}");
                go.transform.SetParent(transform, false);
                go.AddComponent<SpriteRenderer>();
                var a = go.AddComponent<FSMNPCAgent>();
                a.Init(settings, _fsmWaypoints[i]);
                a.ResetAgent(_fsmStartPos[i]);
                _fsm.Add(a);
            }

            for (int i = 0; i < settings.btCount; i++)
            {
                var go = new GameObject($"BT_{i}");
                go.transform.SetParent(transform, false);
                go.AddComponent<SpriteRenderer>();
                var a = go.AddComponent<BTNPCAgent>();
                a.Init(settings, _btWaypoints[i]);
                a.ResetAgent(_btStartPos[i]);
                _bt.Add(a);
            }
        }

    }
}
