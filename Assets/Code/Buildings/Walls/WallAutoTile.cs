using UnityEngine;

namespace CT.Buildings
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class WallAutoTile : MonoBehaviour
    {
        public WallAtlas atlas;

        private SpriteRenderer sr;
        private Vector2Int cell;
        private bool registered;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        void OnEnable()
        {
            if (WallManager.Grid == null) return;
            cell = WallManager.Grid.WorldToCell(transform.position);
            Register();
        }

        void OnDisable()
        {
            Unregister();
        }

        void OnDestroy()
        {
            Unregister();
        }

        void Register()
        {
            if (registered) return;
            registered = true;
            WallManager.Register(this, cell);
        }

        void Unregister()
        {
            if (!registered) return;
            registered = false;
            WallManager.Unregister(cell);
        }

        // Compute EXPOSED sides mask (N=1, E=2, S=4, W=8). Exposed = NO wall neighbor.
        public void Refresh()
        {
            if (!atlas || WallManager.Grid == null) return;

            int mask = 0;
            // Neighbor cells
            var N = cell + Vector2Int.up;
            var E = cell + Vector2Int.right;
            var S = cell + Vector2Int.down;
            var W = cell + Vector2Int.left;

            // If out of bounds OR no wall there => exposed
            if (!WallManager.Grid.InBounds(N) || !WallManager.HasWall(N)) mask |= 1;
            if (!WallManager.Grid.InBounds(E) || !WallManager.HasWall(E)) mask |= 2;
            if (!WallManager.Grid.InBounds(S) || !WallManager.HasWall(S)) mask |= 4;
            if (!WallManager.Grid.InBounds(W) || !WallManager.HasWall(W)) mask |= 8;

            var sprite = atlas.ForMask(mask);
            if (sprite && sr.sprite != sprite) sr.sprite = sprite;
        }
    }
}

