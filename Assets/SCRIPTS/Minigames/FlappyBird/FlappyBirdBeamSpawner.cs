using System;
using System.Collections.Generic;
using UnityEngine;

namespace Petropolis.Minigames.FlappyBird
{
    /// <summary>
    /// Spawns <see cref="FlappyBirdBeam"/> obstacles at a fixed horizontal cadence. Each beam's
    /// gap center Y is picked at random from <see cref="minGapCenterY"/>..<see cref="maxGapCenterY"/>,
    /// with a non-repeating constraint: every new gap center must differ from the previous one
    /// by at least <see cref="minGapDelta"/>. The spawner builds the beam entirely at runtime
    /// (no prefab), so the scene stays self-contained.
    /// </summary>
    public class FlappyBirdBeamSpawner : MonoBehaviour
    {
        [Header("Spawn layout")]
        [SerializeField] float spawnX = 10f;
        [SerializeField] float destroyAtX = -12f;
        [SerializeField] float spawnInterval = 1.6f;
        [SerializeField] float scrollSpeed = 3.5f;

        [Header("Gap shape")]
        [SerializeField] float gapSize = 3.2f;
        [SerializeField] float minGapCenterY = -2.2f;
        [SerializeField] float maxGapCenterY = 2.2f;
        [Tooltip("Minimum absolute difference between successive gap centers — keeps consecutive beams from feeling the same.")]
        [SerializeField] float minGapDelta = 1.2f;
        [SerializeField] float beamWidth = 2.2f;
        [SerializeField] float beamHeight = 10f;

        [Header("Styling")]
        [SerializeField] Color beamColor = new Color(0.25f, 0.78f, 0.35f, 1f);

        public event Action<FlappyBirdBeam> BeamPassed;

        readonly List<FlappyBirdBeam> liveBeams = new();
        float timer;
        bool spawning;
        float? previousGapCenter;

        public void BeginSpawning()
        {
            ClearAll();
            timer = 0.5f;
            previousGapCenter = null;
            spawning = true;
            SetMovingForAll(true);
        }

        public void StopSpawning()
        {
            spawning = false;
            SetMovingForAll(false);
        }

        public void ClearAll()
        {
            for (int i = liveBeams.Count - 1; i >= 0; i--)
            {
                if (liveBeams[i] != null) Destroy(liveBeams[i].gameObject);
            }
            liveBeams.Clear();
        }

        void SetMovingForAll(bool moving)
        {
            for (int i = 0; i < liveBeams.Count; i++)
            {
                if (liveBeams[i] != null) liveBeams[i].SetMoving(moving);
            }
        }

        void Update()
        {
            if (!spawning) return;
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                SpawnOne();
                timer += spawnInterval;
            }
        }

        void SpawnOne()
        {
            float gapCenter = PickNonRepeatingGapCenter();
            previousGapCenter = gapCenter;

            var root = new GameObject("Beam");
            root.transform.SetParent(transform, worldPositionStays: false);
            root.transform.position = new Vector3(spawnX, 0f, 0f);

            var beam = root.AddComponent<FlappyBirdBeam>();
            beam.Configure(scrollSpeed, destroyAtX);
            beam.Passed += OnBeamPassed;

            float topY = gapCenter + gapSize * 0.5f + beamHeight * 0.5f;
            float bottomY = gapCenter - gapSize * 0.5f - beamHeight * 0.5f;
            CreateBeamPart(root.transform, "Top", new Vector3(0f, topY, 0f));
            CreateBeamPart(root.transform, "Bottom", new Vector3(0f, bottomY, 0f));
            CreateScoreTrigger(root.transform, gapCenter);

            liveBeams.Add(beam);
        }

        float PickNonRepeatingGapCenter()
        {
            for (int attempts = 0; attempts < 8; attempts++)
            {
                float candidate = UnityEngine.Random.Range(minGapCenterY, maxGapCenterY);
                if (!previousGapCenter.HasValue || Mathf.Abs(candidate - previousGapCenter.Value) >= minGapDelta)
                {
                    return candidate;
                }
            }
            float prev = previousGapCenter ?? 0f;
            float flipped = prev >= 0f ? minGapCenterY : maxGapCenterY;
            return Mathf.Clamp(flipped, minGapCenterY, maxGapCenterY);
        }

        void CreateBeamPart(Transform parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(beamWidth, beamHeight, 1f);
            go.tag = "FlappyBirdObstacle";

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = FlappyBirdSpriteCache.WhiteSquare;
            sr.color = beamColor;
            sr.sortingOrder = 1;

            go.AddComponent<BoxCollider2D>();
        }

        void CreateScoreTrigger(Transform parent, float gapCenter)
        {
            var go = new GameObject("ScoreTrigger");
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = new Vector3(0f, gapCenter, 0f);
            go.transform.localScale = new Vector3(beamWidth * 0.2f, gapSize * 0.9f, 1f);

            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;

            go.AddComponent<FlappyBirdBeamScoreTrigger>();
        }

        void OnBeamPassed(FlappyBirdBeam beam)
        {
            BeamPassed?.Invoke(beam);
        }
    }

    /// <summary>
    /// Tiny helper that hands out a reusable 1x1 white sprite so beam parts can render
    /// without any project assets — keeps the minigame scene self-contained.
    /// </summary>
    static class FlappyBirdSpriteCache
    {
        static Sprite whiteSquare;
        public static Sprite WhiteSquare
        {
            get
            {
                if (whiteSquare != null) return whiteSquare;
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false)
                {
                    name = "FlappyBirdWhitePixel",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave,
                };
                tex.SetPixel(0, 0, Color.white);
                tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
                whiteSquare = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), pixelsPerUnit: 1f);
                whiteSquare.name = "FlappyBirdWhiteSquare";
                whiteSquare.hideFlags = HideFlags.HideAndDontSave;
                return whiteSquare;
            }
        }
    }
}
