using UnityEngine;

namespace CT.Buildings
{
    [CreateAssetMenu(fileName = "WallAtlas", menuName = "CT/Walls/Wall Atlas")]
    public class WallAtlas : ScriptableObject
    {
        [Header("Sprites by EXPOSED sides (bitmask: N=1, E=2, S=4, W=8)")]
        public Sprite wall_center;                    // exposed: none (mask=0)
        public Sprite wall_edge_N, wall_edge_E, wall_edge_S, wall_edge_W; // 1,2,4,8
        public Sprite wall_corner_NE, wall_corner_NW, wall_corner_SE, wall_corner_SW; // 1|2, 1|8, 2|4, 4|8
        public Sprite wall_T_N, wall_T_E, wall_T_S, wall_T_W;             // 1|2|8, 1|2|4, 2|4|8, 1|4|8
        public Sprite wall_cross;                                         // 1|2|4|8

        [Header("Optional straights (opposite exposures). Fallbacks to center if null.")]
        public Sprite wall_straight_NS; // mask = 1|4
        public Sprite wall_straight_EW; // mask = 2|8

        public Sprite ForMask(int mask)
        {
            switch (mask)
            {
                case 0: return wall_center;
                case 1: return wall_edge_N;
                case 2: return wall_edge_E;
                case 4: return wall_edge_S;
                case 8: return wall_edge_W;

                case 1 | 2: return wall_corner_NE;
                case 1 | 8: return wall_corner_NW;
                case 2 | 4: return wall_corner_SE;
                case 4 | 8: return wall_corner_SW;

                case 1 | 2 | 8: return wall_T_N; // open to N
                case 1 | 2 | 4: return wall_T_E; // open to E
                case 2 | 4 | 8: return wall_T_S; // open to S
                case 1 | 4 | 8: return wall_T_W; // open to W

                case 1 | 4: return wall_straight_EW ? wall_straight_EW : (wall_center ? wall_center : wall_edge_N);
                case 2 | 8: return wall_straight_NS ? wall_straight_NS : (wall_center ? wall_center : wall_edge_E);

                case 1 | 2 | 4 | 8: return wall_cross;
            }
            return wall_center;
        }
    }
}

