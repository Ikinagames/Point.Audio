// Copyright 2022 Ikina Games
// Author : Seung Ha Kim (Syadeu)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Point.Collections;
using System.Collections.Generic;
using Unity.Collections;

namespace Point.Audio
{
    /// Material type that determines the acoustic properties of a room surface.
    public enum SurfaceMaterial
    {
        Transparent = 0,              ///< Transparent
        AcousticCeilingTiles = 1,     ///< Acoustic ceiling tiles
        BrickBare = 2,                ///< Brick, bare
        BrickPainted = 3,             ///< Brick, painted
        ConcreteBlockCoarse = 4,      ///< Concrete block, coarse
        ConcreteBlockPainted = 5,     ///< Concrete block, painted
        CurtainHeavy = 6,             ///< Curtain, heavy
        FiberglassInsulation = 7,     ///< Fiberglass insulation
        GlassThin = 8,                ///< Glass, thin
        GlassThick = 9,               ///< Glass, thick
        Grass = 10,                   ///< Grass
        LinoleumOnConcrete = 11,      ///< Linoleum on concrete
        Marble = 12,                  ///< Marble
        Metal = 13,                   ///< Galvanized sheet metal
        ParquetOnConcrete = 14,       ///< Parquet on concrete
        PlasterRough = 15,            ///< Plaster, rough
        PlasterSmooth = 16,           ///< Plaster, smooth
        PlywoodPanel = 17,            ///< Plywood panel
        PolishedConcreteOrTile = 18,  ///< Polished concrete or tile
        Sheetrock = 19,               ///< Sheetrock
        WaterOrIceSurface = 20,       ///< Water or ice surface
        WoodCeiling = 21,             ///< Wood ceiling
        WoodPanel = 22                ///< Wood panel
    }
}
