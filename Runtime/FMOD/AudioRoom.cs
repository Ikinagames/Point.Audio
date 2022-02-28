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


namespace Point.Audio
{
    public struct AudioRoom
    {
        // Center position of the room in world space.
        public float positionX;
        public float positionY;
        public float positionZ;

        // Rotation (quaternion) of the room in world space.
        public float rotationX;
        public float rotationY;
        public float rotationZ;
        public float rotationW;

        // Size of the shoebox room in world space.
        public float dimensionsX;
        public float dimensionsY;
        public float dimensionsZ;

        // Material name of each surface of the shoebox room.
        public SurfaceMaterial materialLeft;
        public SurfaceMaterial materialRight;
        public SurfaceMaterial materialBottom;
        public SurfaceMaterial materialTop;
        public SurfaceMaterial materialFront;
        public SurfaceMaterial materialBack;

        // User defined uniform scaling factor for reflectivity. This parameter has no effect when set
        // to 1.0f.
        public float reflectionScalar;

        // User defined reverb tail gain multiplier. This parameter has no effect when set to 0.0f.
        public float reverbGain;

        // Adjusts the reverberation time across all frequency bands. RT60 values are multiplied by this
        // factor. Has no effect when set to 1.0f.
        public float reverbTime;

        // Controls the slope of a line from the lowest to the highest RT60 values (increases high
        // frequency RT60s when positive, decreases when negative). Has no effect when set to 0.0f.
        public float reverbBrightness;
    }
}
