module particle
open vector
    type particle = 
        {
            position : vector3
            velocity : vector3
            acceleration : vector3
            damping : float
            forceAccumulator : vector3
        }

