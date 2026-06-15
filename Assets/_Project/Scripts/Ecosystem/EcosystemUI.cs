using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BehaviorSimulation.Core;

namespace BehaviorSimulation.Ecosystem
{
    public class EcosystemUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EcosystemManager  manager;
        [SerializeField] private SimulationController simController;

        [Header("Buttons")]
        [SerializeField] private Button playPauseButton;
        [SerializeField] private Button resetButton;

        [Header("Stats text")]
        [SerializeField] private TMP_Text preyCountText;
        [SerializeField] private TMP_Text predCountText;

        [Header("Population graph")]
        [SerializeField] private RawImage graphImage;

        // ── Graph internals ───────────────────────────────────────────────────────
        const int GraphW = 200, GraphH = 60;

        Texture2D   _graphTex;
        Color32[]   _graphPix;
        int[]       _preyHistory  = new int[GraphW];
        int[]       _predHistory  = new int[GraphW];
        int         _head;
        int         _maxSeen      = 1;

        static readonly Color32 BgCol    = new(18, 18, 22, 255);
        static readonly Color32 PreyCol  = new(60, 210, 90, 255);
        static readonly Color32 PredCol  = new(210, 50, 50, 255);
        static readonly Color32 BlendCol = new(180, 140, 60, 255); // overlap = orange

        // ── Unity ─────────────────────────────────────────────────────────────────

        void Start()
        {
            playPauseButton?.onClick.AddListener(OnPlayPause);
            resetButton?.onClick.AddListener(() =>
            {
                simController?.ResetSimulation();
                ClearGraph();
            });

            _graphTex = new Texture2D(GraphW, GraphH, TextureFormat.RGB24, false)
                        { filterMode = FilterMode.Point };
            _graphPix = new Color32[GraphW * GraphH];
            ClearGraph();
            if (graphImage) graphImage.texture = _graphTex;

            if (manager != null)
                manager.OnPopulationChanged += OnPop;
        }

        void OnDestroy()
        {
            if (manager != null)
                manager.OnPopulationChanged -= OnPop;
        }

        void Update() => RefreshPlayLabel();

        // ── Callbacks ─────────────────────────────────────────────────────────────

        void OnPlayPause()
        {
            simController?.TogglePlayPause();
            RefreshPlayLabel();
        }

        void OnPop(int prey, int pred)
        {
            if (preyCountText) preyCountText.text = $"Prey:  {prey}";
            if (predCountText) predCountText.text  = $"Pred:  {pred}";
            AddSample(prey, pred);
        }

        // ── Graph ─────────────────────────────────────────────────────────────────

        void AddSample(int prey, int pred)
        {
            _preyHistory[_head] = prey;
            _predHistory[_head] = pred;
            _head = (_head + 1) % GraphW;

            _maxSeen = Mathf.Max(_maxSeen, prey, pred);
            RedrawGraph();
        }

        void RedrawGraph()
        {
            for (int i = 0; i < _graphPix.Length; i++) _graphPix[i] = BgCol;

            for (int x = 0; x < GraphW; x++)
            {
                int idx  = (_head + x) % GraphW;
                int prey = _preyHistory[idx];
                int pred = _predHistory[idx];

                int preyH = prey * GraphH / _maxSeen;
                int predH = pred * GraphH / _maxSeen;

                for (int y = 0; y < GraphH; y++)
                {
                    bool hasPrey = y < preyH;
                    bool hasPred = y < predH;
                    if (hasPrey && hasPred) _graphPix[y * GraphW + x] = BlendCol;
                    else if (hasPrey)       _graphPix[y * GraphW + x] = PreyCol;
                    else if (hasPred)       _graphPix[y * GraphW + x] = PredCol;
                }
            }

            _graphTex.SetPixels32(_graphPix);
            _graphTex.Apply(false);
        }

        void ClearGraph()
        {
            Array.Clear(_preyHistory, 0, GraphW);
            Array.Clear(_predHistory, 0, GraphW);
            _head    = 0;
            _maxSeen = 1;
            for (int i = 0; i < _graphPix.Length; i++) _graphPix[i] = BgCol;
            _graphTex?.SetPixels32(_graphPix);
            _graphTex?.Apply(false);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        void RefreshPlayLabel()
        {
            if (playPauseButton == null) return;
            var lbl = playPauseButton.GetComponentInChildren<TMP_Text>();
            if (lbl)
                lbl.text = (simController != null && simController.IsPlaying) ? "Pause" : "Play";
        }
    }
}
