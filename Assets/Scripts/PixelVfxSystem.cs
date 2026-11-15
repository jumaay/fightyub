using UnityEngine;

namespace Fightyub
{
    public enum PixelImpactKind
    {
        Punch,
        Kick,
        Block,
        PerfectBlock,
        Special
    }

    public enum PixelShape
    {
        Square,
        Diamond,
        Streak,
        Ring
    }

    /// <summary>
    /// Pooled, zero-allocation 16-bit pixel-art particle emitter driven by FighterController.TickFighter(dt).
    /// Respects pause (stops updating when dt == 0 / paused) and clears cleanly on round/match reset.
    /// </summary>
    public class PixelVfxEmitter
    {
        private struct Particle
        {
            public bool active;
            public GameObject go;
            public SpriteRenderer sr;
            public Vector3 pos;
            public Vector3 vel;
            public float gravity;
            public float drag;
            public float age;
            public float lifetime;
            public float startScale;
            public float endScale;
            public Color startColor;
            public Color endColor;
        }

        private const int PoolSize = 48;
        private readonly Particle[] _pool;
        private readonly GameObject _root;
        private int _cursor = 0;

        private static Sprite _squareSprite;
        private static Sprite _diamondSprite;
        private static Sprite _streakSprite;
        private static Sprite _ringSprite;

        public int ActiveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _pool.Length; i++)
                {
                    if (_pool[i].active) count++;
                }
                return count;
            }
        }

        public PixelVfxEmitter(string ownerName, Transform parentTransform)
        {
            EnsureSprites();

            string rootName = $"{ownerName}_PixelVFXPool";
            var existing = GameObject.Find(rootName);
            _root = existing != null ? existing : new GameObject(rootName);
            if (parentTransform != null && _root.transform.parent != parentTransform)
            {
                _root.transform.SetParent(parentTransform, false);
            }

            _pool = new Particle[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                Transform existingChild = _root.transform.childCount > i ? _root.transform.GetChild(i) : null;
                GameObject pGo = existingChild != null ? existingChild.gameObject : new GameObject($"P_{i}");
                if (pGo.transform.parent != _root.transform)
                {
                    pGo.transform.SetParent(_root.transform, false);
                }

                var sr = pGo.GetComponent<SpriteRenderer>();
                if (sr == null) sr = pGo.AddComponent<SpriteRenderer>();
                sr.enabled = false;

                _pool[i] = new Particle
                {
                    active = false,
                    go = pGo,
                    sr = sr
                };
            }
        }

        public void Emit(
            Vector3 worldPos,
            Vector3 velocity,
            Color startColor,
            Color endColor,
            float lifetime,
            float startScale,
            float endScale,
            PixelShape shape,
            int sortingLayerId,
            int sortingOrder,
            float gravity = 0f,
            float drag = 0f,
            float rotationDeg = 0f)
        {
            int idx = _cursor;
            _cursor = (_cursor + 1) % PoolSize;

            ref Particle p = ref _pool[idx];
            if (p.go == null || p.sr == null) return;

            p.active = true;
            p.pos = worldPos;
            p.vel = velocity;
            p.gravity = gravity;
            p.drag = drag;
            p.age = 0f;
            p.lifetime = Mathf.Max(0.02f, lifetime);
            p.startScale = startScale;
            p.endScale = endScale;
            p.startColor = startColor;
            p.endColor = endColor;

            p.sr.sprite = GetSprite(shape);
            p.sr.sortingLayerID = sortingLayerId;
            p.sr.sortingOrder = sortingOrder;
            p.sr.color = startColor;
            p.sr.enabled = true;

            p.go.transform.position = worldPos;
            p.go.transform.localScale = new Vector3(startScale, startScale, 1f);
            p.go.transform.rotation = Quaternion.Euler(0f, 0f, rotationDeg);
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;

            for (int i = 0; i < PoolSize; i++)
            {
                ref Particle p = ref _pool[i];
                if (!p.active) continue;

                p.age += dt;
                if (p.age >= p.lifetime)
                {
                    p.active = false;
                    if (p.sr != null) p.sr.enabled = false;
                    continue;
                }

                if (p.gravity != 0f)
                {
                    p.vel.y -= p.gravity * dt;
                }
                if (p.drag > 0f)
                {
                    float damp = Mathf.Clamp01(1f - p.drag * dt);
                    p.vel *= damp;
                }

                p.pos += p.vel * dt;
                float t = Mathf.Clamp01(p.age / p.lifetime);

                // Stepped 16-bit feel (4 discrete steps for scale/color)
                float steppedT = Mathf.Floor(t * 4f) * 0.25f;
                float sc = Mathf.Lerp(p.startScale, p.endScale, steppedT);

                if (p.go != null)
                {
                    p.go.transform.position = p.pos;
                    p.go.transform.localScale = new Vector3(sc, sc, 1f);
                }
                if (p.sr != null)
                {
                    p.sr.color = Color.Lerp(p.startColor, p.endColor, t);
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i < PoolSize; i++)
            {
                _pool[i].active = false;
                if (_pool[i].sr != null) _pool[i].sr.enabled = false;
            }
        }

        public void DestroyPool()
        {
            Clear();
            if (_root != null)
            {
                Object.Destroy(_root);
            }
        }

        private static Sprite GetSprite(PixelShape shape)
        {
            EnsureSprites();
            switch (shape)
            {
                case PixelShape.Diamond: return _diamondSprite;
                case PixelShape.Streak: return _streakSprite;
                case PixelShape.Ring: return _ringSprite;
                default: return _squareSprite;
            }
        }

        private static void EnsureSprites()
        {
            if (_squareSprite != null && _diamondSprite != null && _streakSprite != null && _ringSprite != null)
            {
                return;
            }

            const float ppu = 32f;

            // 1. 4x4 Square pixel block
            if (_squareSprite == null)
            {
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                var px = new Color[16];
                for (int i = 0; i < 16; i++) px[i] = Color.white;
                tex.SetPixels(px);
                tex.Apply();
                _squareSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), ppu);
                _squareSprite.name = "PixelVfx_Square";
            }

            // 2. 5x5 Diamond / Star pixel
            if (_diamondSprite == null)
            {
                var tex = new Texture2D(5, 5, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                var px = new Color[25];
                for (int y = 0; y < 5; y++)
                {
                    for (int x = 0; x < 5; x++)
                    {
                        int dist = Mathf.Abs(x - 2) + Mathf.Abs(y - 2);
                        px[y * 5 + x] = (dist <= 2) ? Color.white : Color.clear;
                    }
                }
                tex.SetPixels(px);
                tex.Apply();
                _diamondSprite = Sprite.Create(tex, new Rect(0, 0, 5, 5), new Vector2(0.5f, 0.5f), ppu);
                _diamondSprite.name = "PixelVfx_Diamond";
            }

            // 3. 7x2 Horizontal Streak pixel
            if (_streakSprite == null)
            {
                var tex = new Texture2D(7, 2, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                var px = new Color[14];
                for (int y = 0; y < 2; y++)
                {
                    for (int x = 0; x < 7; x++)
                    {
                        px[y * 7 + x] = (y == 0 && (x == 0 || x == 6)) ? new Color(1f, 1f, 1f, 0.65f) : Color.white;
                    }
                }
                tex.SetPixels(px);
                tex.Apply();
                _streakSprite = Sprite.Create(tex, new Rect(0, 0, 7, 2), new Vector2(0.5f, 0.5f), ppu);
                _streakSprite.name = "PixelVfx_Streak";
            }

            // 4. 7x7 Hollow Pixel Ring / Bubble
            if (_ringSprite == null)
            {
                var tex = new Texture2D(7, 7, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                var px = new Color[49];
                for (int y = 0; y < 7; y++)
                {
                    for (int x = 0; x < 7; x++)
                    {
                        float d = Mathf.Sqrt((x - 3) * (x - 3) + (y - 3) * (y - 3));
                        px[y * 7 + x] = (d >= 1.9f && d <= 3.1f) ? Color.white : Color.clear;
                    }
                }
                tex.SetPixels(px);
                tex.Apply();
                _ringSprite = Sprite.Create(tex, new Rect(0, 0, 7, 7), new Vector2(0.5f, 0.5f), ppu);
                _ringSprite.name = "PixelVfx_Ring";
            }
        }
    }
}
