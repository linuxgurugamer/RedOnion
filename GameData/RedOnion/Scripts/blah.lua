var targetAltitude = 80   // target orbit's altitude in km
var ascentHeading  = 90   // ascent heading (0 = north, 90 = equitorial)
var ascentRoll = math.round((ship.pitch < 80 ? ship.roll :
  180 - ship.up.angle(ship.north.rotate ascentHeading, ship.away)) / 45)*45
var circleRoll = ascentRoll
var ascentProfile  = 5    // profile (1-9, less means less aggresive turn)
var speedRatio     = 5    // speed/altitude ratio to base the curve on (higher is for speed, more than 10 means switch earlier to full speed-based)
var maxSpeed       = .9   // target escape speed (fraction of minimal or in m/s if above 2)
var safeAltitude   = 2 + math.max atmosphere.depth/1000, 8 // safe altitude above atmosphere and mountains
var startAngle     = math.round(ship.pitch,1) // usually 90 but e.g. DynaWing starts with 81.2
var firstAngle     = 60   // angle upto which to use simple ratio
var firstAltitude  = math.max safeAltitude/10, 3 // altitude (in km) of first phase (to reach pitch=firstAngle)
var narrowAltitude = 100  // altitude (in m) upto which to go straight up

if startAngle > 89
  startAngle = 90

//: PARAM GUI

var firda = true
var evan = false
var cust = false
def gui
  var lblWidth = 120
  var boxWidth = 60
  var uniWidth = 40
  var wnd = new ui.window "Launch " + ship.name
  wnd.x -= (unity.screen.width - 200) / 3
  var hintLabel = new ui.label
  def boxEnter box
    hintLabel.text = box.tag
  def row text, value, units, hint
    var row = wnd.addHorizontal()
    var lbl = row.addLabel text
    lbl.minWidth = lblWidth
    var box = row.addTextBox string value
    box.tag = hint
    box.enter.add boxEnter
    if units == null
      box.preferWidth = boxWidth + 3 + uniWidth
    else
      box.preferWidth = boxWidth
      var uni = row.add new ui.label
      uni.text = units
      uni.minWidth = uniWidth
    return box
  
  var ta = row "Target altitude: ",  targetAltitude, "km",    "Final Apoapsis"
  var ah = row "Ascent heading: ",   ascentHeading,  "°",     "Course during ascent (not inclination)"
  var ar = row "Ascent roll: ",      ascentRoll,     "°",     "Roll/bank during ascent"
  var cr = row "Circularize roll: ", circleRoll,     "°",     "Roll/bank during circularization"
  var pf = row "Ascent profile: ",   ascentProfile,  "1-9",   "Lower means less agressive turn"
  var pr = row "Speed/Alt ratio: ",  speedRatio,     "*10%",  "Higher for speed, above 10 means earlier"
  var ms = row "Max. Speed: ",       maxSpeed,       "*|m/s", "<= 2 for fraction of minimal, or m/s"
  var sa = row "Safe altitude: ",    safeAltitude,   "km",    "Above atmposhepre and mountains"
  var sd = row "Start angle: ",      startAngle,     "°",     "Start degrees of pitch"
  var fa = row "First angle: ",      firstAngle,     "°",     "Degrees of pitch for first phase"
  var a1 = row "First altitude: ",   firstAltitude,  "km",    "Altitude for first phase"
  var na = row "Narrow altitude: ",  narrowAltitude,  "m",    "Minimal altitude to go straight up"
  
  var apsel = wnd.addHorizontal()
  apsel.addLabel "Pilot: "
  var xfirda = apsel.addExclusive "Firda"
  var xevan  = apsel.addExclusive "Evan"
  var xcust  = apsel.addExclusive "Custom"
  xfirda.pressed = true
  
  wnd.add hintLabel
  var brow = wnd.addHorizontal()
  var selected = 0
  wnd.closed += def => selected = 1
  brow.addButton("Cancel",  def => selected = 1).flexWidth = 1
  brow.addButton("Control", def => selected = 2).flexWidth = 1
  brow.addButton("Launch",  def => selected = 3).flexWidth = 1
  
  while selected == 0
    wait
  
  if selected == 2 // control
    wnd.dispose()
    run.replace "control.ros"
    //script not continuing here, replaced by control.ros
  
  if selected == 3 // launch
    targetAltitude = double ta.text
    ascentHeading  = double ah.text
    ascentRoll     = double ar.text
    circleRoll     = double cr.text
    ascentProfile  = double pf.text
    speedRatio     = double pr.text
    maxSpeed       = double ms.text
    safeAltitude   = double sa.text
    startAngle     = double sd.text
    firstAngle     = double fa.text
    firstAltitude  = double a1.text
    narrowAltitude = double na.text

  wnd.dispose
  firda = xfirda.pressed
  evan  = xevan.pressed
  cust  = xcust.pressed
  return selected == 3
if not gui()
  return

targetAltitude *= 1000
safeAltitude   *= 1000
firstAltitude  *= 1000
ascentProfile  = 1-math.clamp(ascentProfile, 1, 9)*0.1
speedRatio     = speedRatio*0.1
if maxSpeed <= 2
  maxSpeed *= math.sqrt body.mu/(body.radius+safeAltitude)


//: STEERING

def setPitchFirda pitch
  ship.pitch = pitch
def setPitchEvan pitch
  ksp.flightControl.setRel ascentHeading, pitch
var setPitch = evan ? setPitchEvan : setPitchFirda

var customSteering = null
if not evan
  autopilot.killRot = true
  if cust
    def steerPID
      var reg = PID 1.0, 0.5, 0.01, 0.4
      reg.maxInput = 1
      reg.minInput = -1
      reg.maxOutput = 1
      reg.minOutput = -1
      reg.outputChangeLimit = 5
      reg.accumulatorLimit = 0.5
      reg.errorLimit = 1
      return reg
    var ppid = steerPID()
    var ypid = steerPID()
    var rpid = steerPID()
    customSteering = def
      var av = ship.angularVelocity
      var ma = ship.maxAngular
      
      var di = ship.local autopilot.direction
      var pd = math.deg.atan2 -di.z, di.y
      var yd = math.deg.atan2  di.x, di.y
      var ps = av.x * (math.abs(av.x) / ma.x + 0.5)
      var ys = av.z * (math.abs(av.z) / ma.z + 0.5)
      ppid.input = 10 * (pd + ps) / ma.x
      ypid.input = 10 * (yd + ys) / ma.z
      autopilot.rawPitch = ppid.update()
      autopilot.rawYaw   = ypid.update()
      print "Diff {0,6:F2}:{1,6:F2} Ctrl {2,6:F1}:{3,6:F1} Q:{4:F2}",
        pd, yd, ppid.output * 100, ypid.output * 100, ship.q
      print "Stop {0,6:F2}:{1,6:F2} Pure {2,6:F1}:{3,6:F1} T:{4:F2}",
        ps, ys, ppid.input  * 100, ypid.input  * 100,
        (ppid.lastDt + ypid.lastDt + rpid.lastDt)/3
      
      var rd = math.clampS180 autopilot.roll - (ship.pitch <= 89 ? ship.roll :
        180.0 + ship.up.angle ship.north.rotate(ascentHeading, ship.away), ship.away)
      var rs = av.y * (math.abs(av.y) / ma.y + 0.5)
      rpid.input = 10 * (rd + rs) / ma.y
      autopilot.rawRoll  = rpid.update()
      print "Roll {0,6:F2}/{1,6:F2} Ctrl {2,6:F1}/{3,6:F1} T:{4:F2}",
        rd, rs, rpid.output * 100, rpid.input  * 100, rpid.lastDt

var minSpeed = null
var prevSteerReport = -inf
def steer
  if ship.altitude <= narrowAltitude
    setPitch startAngle
    return
  if ship.periapsis >= safeAltitude
    setPitch 0
    return
//limit AoA when Q is high
  var srfA = 90 - ship.away.angle ship.surfaceVelocity
  var curr = srfA
  var maxA = 3.0/math.clamp(ship.q, 0.1, 0.75) - 3.0
//smooth transition from surface to orbital speed by ratio of altitude to safe altitude
  var factor = math.clamp (altitude/safeAltitude)^2, 0, 1
  var speed = factor * ship.velocity.size + ship.surfaceVelocity.size * (1-factor)
  if minSpeed == null then minSpeed = speed
  var want
  if ship.altitude <= firstAltitude
    want = startAngle - (startAngle - firstAngle) * ship.altitude/firstAltitude
  else
  //mix of speed-based and altitude-based curve
    var altRatio = math.clamp (ship.altitude-firstAltitude)/(safeAltitude-firstAltitude), 0, 1
    var ratio = math.min 1, speedRatio * altRatio
    var fract = ((1-ratio)*altRatio + ratio *
      math.min(1,(speed-minSpeed)/(maxSpeed-minSpeed)))^ascentProfile
    want = firstAngle * (1 - fract)
//compensate for differences from desired prograde pitch
  var pitch
  curr = factor * (90 - ship.away.angle ship.velocity) + (1-factor) * srfA
  if want < curr and ship.altitude < safeAltitude/3
    pitch = want + 3*ship.altitude/safeAltitude*(want-curr)
  else pitch = 2*want-curr
//final adjustment
  var ctrl = math.clamp pitch, math.max(0, srfA-maxA), math.min(srfA+maxA, 90)
  setPitch ctrl
  if time.since(prevSteerReport) >= 1
    prevSteerReport = time()
    print "Ctrl:{0,6:F2} Want:{1,6:F2} Have:{2,6:F2} Q:{3,4:F2} ", ctrl, want, ship.pitch, ship.q
    print "ReqA:{0,6:F2} Curr:{1,6:F2} SrfA:{2,6:F2} MaxA:{3,6:F2} ", pitch, curr, srfA, maxA


//: THROTTLE

var lastApoAbove = 0.0
def throttle
  if ship.apoapsis >= targetAltitude
    ship.throttle = 0
    if lastApoAbove == 0.0
      print "Apoapsis reached"
    lastApoAbove = time()
  else if lastApoAbove != 0 and time() - lastApoAbove < 10
    ship.throttle = 0
  else
    var power = 1
    if ship.apoapsis > safeAltitude*0.9 and ship.altitude < safeAltitude
      power = math.max 0.1, (targetAltitude-ship.apoapsis) / math.max 1.0, targetAltitude-safeAltitude*0.9
    if ship.apoapsis > targetAltitude * 0.95
      power = math.min power, 0.01+20*(1-ship.apoapsis/targetAltitude)
    ship.throttle = math.min power, user.throttle


//: STAGING

def staging
  if not stage.ready
    return
  var nextDecoupler = ship.parts.nextDecoupler
  if nextDecoupler == null
  //nothing to separate
    if stage.pending
    //but there appears to be inactive engine
    //TODO: add a check like stage.next.containsType("engine")
      print "Pending last stage"
      stage
    return
  if nextDecoupler.isType("LaunchClamp")
    print "Releasing launch clamps"
    stage
    return
  if not stage.engines.anyOperational
    print "No operational engine"
    stage
    return
  if stage.fuel <= 0.1
    print "Stage out of fuel"
    stage
    return


//: VECTORS

var vd = [
  new vector.draw,
  new vector.draw,
  new vector.draw,
  new vector.draw]
vd[0].scale = 15
vd[0].width = .5
vd[1].color = ui.color.blue
vd[1].scale = 10
vd[2].color = ui.color.green
vd[2].width = .3
vd[2].scale = 10
vd[3].color = ui.color.red
vd[3].width = .2
vd[3].scale = 12
def vectors
  vd[0].vector = ship.forward
  vd[1].vector = ship.autopilot.direction.normalized
  vd[2].vector = ship.velocity.normalized
  vd[3].vector = ship.srfVel.normalized
vectors
for var d in vd
  d.show


//: ASCENT

ship.throttle = 0
user.throttle = 1
if not evan
  ship.heading = ascentHeading
  ship.roll = ascentRoll
var ctlsub = system.update def
  throttle()
  steer()
var stgsub = system.idle staging
var vdraws = system.update vectors
var steers = customSteering == null ? null :
  system.update customSteering

print "Target apoapsis: " + targetAltitude
print "Safe altitude:   " + safeAltitude
until ship.altitude >= safeAltitude and ship.apoapsis >= targetAltitude*.99
  wait

ctlsub.remove()
user.throttle = 0
ship.autopilot.disable
print "Safe altitude reached"
vd[3].hide //srfvel


//: CIRCULARIZE

def dt => ship.eccentricity < 0.001 or
  ship.periapsis < safeAltitude and ship.timeToAp > ship.timeToPe ?
  0 : ship.timeToAp
var minE = ship.eccentricity
def dv
  minE = math.min minE, ship.eccentricity
  if minE < 0.001 and (minE < 0.00001 or ship.eccentricity > 1.1*minE)
    return vector.zero
  var p = ship.position-body.position
  var v1 = ship.velocity
  var v2 = math.sqrt(body.mu/p.size) * (p.cross v1.cross p).normalized
  return v2-v1

if not evan
  autopilot.roll = circleRoll
ctlsub = system.update def
  autopilot.direction = ship.away.cross ship.velocity.cross ship.away

var maxThrottle = 0.0
def circle
  var pwr = 1
  var tta = ship.timeToAp
  var dir
  if tta < ship.timeToPe
  //minimize velocity vector difference when before apoapsis
    dir = dv()
    pwr = 1 / math.clamp (tta - 0.5 * stage.burnTime dir.size), 1.0, 100.0
    if pwr <= 0.01; pwr = 0.0
  else
  //pitch-up when past apoapsis
    var a = math.clamp(ship.period - tta, 0, 30)
    dir = ship.velocity.rotate a, ship.velocity.cross ship.away
    pwr = 1 + math.tan a
  if evan; ksp.flightControl.targetDir = dir
  else autopilot.direction = dir
  var d = dir.angle ship.forward // direction / face-angle error
  var e = d <= 1 ? 1 : math.deg.cos math.min 89.4, d^2 // min 0.0105
  var needSpeed = math.sqrt(body.mu/(body.radius+ship.apoapsis))
  var haveSpeed = ship.velocity.size
  var need = (needSpeed-haveSpeed) * ship.mass / math.max 1, ship.engines.getThrust()
  autopilot.throttle = pwr * e * math.min(maxThrottle, need)

var prevEtaReport = -10
var switched = false
var warped = false
for
  var bt = stage.burnTime dv().size
  if bt != bt; bt = 120 // bt != bt is test for nan
  var eta = dt()-0.5*bt
  if time.since(prevEtaReport) >= (eta > 10 ? 10 : eta > 5 ? 2 : 1)
    prevEtaReport = time.now
    print "ETA: {0:F1}s", eta
    if eta > 20 and !warped
      warped = true
      time.warp.to prevEtaReport + eta - 10
  if eta <= 5 and not switched
    switched = true
    time.warp.cancel()
    ctlsub.remove
    ctlsub = system.update circle
  if eta <= 1; break
  wait
maxThrottle = 1.0

var almost = null
var choked = null
var warned = 0
var maxHeight = ship.apoapsis*1.01+1000
var prevReport = 0
print "Burn!"
until (ship.eccentricity < 0.00001 // perfect
  or ship.timeToPe < ship.period*0.3 // happens with good accuracy
  or ship.apoapsis > maxHeight and ship.periapsis > math.max(atmosphere.depth,1000)+1000 // something went wrong?
  or ship.apoapsis > maxHeight*1.5+5000) // something went really really wrong

  if ship.eccentricity < 0.01
    if almost == null; almost = time()
    else if time.since(almost) > 10; break
  
  if time.since(prevReport) >= 1
    prevReport = time()
    var delta = dv().size
    var tta = ship.timeToAp
    if tta > ship.timeToPe
      tta -= ship.period
    print "TTA: {0,6:F1} DV: {1,6:F1} BT: {2,6:F1}", tta, delta, stage.burnTime delta
    
  if ship.engines.getThrust() == 0
    if choked == null
      choked = time()
    if time.since(choked) >= 3 and time.since(warned) >= 1
      warned = time()
      print "No acceleration!"
    if time()-choked >= 30
      print "No acceleration!!"
      break
    continue
  choked = null

ctlsub.remove
stgsub.remove
vdraws.remove
if steers != null
  steers.remove
for var d in vd
  d.hide
vd = null
user.throttle = 0
ship.autopilot.disable
print "DONE! e={0:F5} Ap:{1:F3} Pe:{2:F3}", ship.eccentricity, ship.apoapsis*0.001, ship.periapsis*0.001
run.replace "control.ros"
