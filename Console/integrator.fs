module integrator
    open vector
    open particle

    let integrate dt p =
        let nextPosition = dt * p.position
        let nextVelocity = dt * p.acceleration + p.velocity
        let dampedVelocity = p.damping ** dt * nextVelocity
        { 
            p with position = nextPosition; velocity = nextPosition 
        };

    let integrateAll dt particles =
        particles
        |> Seq.map (integrate dt)
