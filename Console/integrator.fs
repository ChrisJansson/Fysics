module integrator
open vector
open particle
    let integrate (dt : float) (p : particle) =
        let nextPosition = dt * p.position
        let nextVelocity = dt * p.acceleration + p.velocity
        let dampedVelocity = p.damping ** dt * nextVelocity
        { p with position = nextPosition; velocity = nextPosition }




