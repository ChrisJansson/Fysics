module vector

    type vector3 = 
        {   x : float
            y : float
            z : float }

        static member (+) (a, b) =
            {   x = a.x + b.x
                y = a.y + b.x
                z = a.z + b.z }

        static member (*) (s, v) =
            {   x = s * v.x
                y = s * v.y
                z = s * v.z }

    let dotProduct a b =
        a.x * b.x +
        a.y * b.y +
        a.z * b.z

    let crossProduct a b =
        {   x = a.y * b.z - a.z * b.y 
            y = a.z * b.x - a.x * b.z
            z = a.x * b.y - a.y * b.x }

