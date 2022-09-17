using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.AI;

public class ThunderMovement : MonoBehaviour {
    public int playerNumber = 1;
    public float turnSpeed = 120f;
    public float turretTurnSpeed = 70f;
    public bool canMove;
    public bool elevLocked;
    public Rigidbody bodyRb;
    public Rigidbody turretRb;
    public Rigidbody cannonRb;
    public HingeJoint bearingHj;
    public HingeJoint elevationHj;

    public bool damageable = true;

    private string movementAxisName;
    private string turnAxisName;
    private float speed = 10f;
    private float movementInputValue;
    private float turnInputValue;
    
    public Transform ammoSpawn;
    public AudioSource _source;
    public AudioClip _clip;
    public ParticleSystem _launchParticles;
    public CameraMovement cameraMovement;
    public GameObject canvasUI;
    public NavMeshAgent agent;
    
    private LineRenderer _line;
    private Vector3 _direction;
    private float _velocity;
    private float _angle;
    private float _time;

    private List<Vector3> _linePoints = new List<Vector3>();
    private List<Vector3> _blastPoints = new List<Vector3>();
    private float _blastRadius;
    private LineRenderer _blastProjection;

    private bool drive;
    private bool neutral;
    private bool reverse;
    private bool park;
    private string gearType;
    private string chargeUnit;
    private int ammoInd;
    private Coroutine coroutine;
    private bool rotatingTank;
    private bool lone;

    private void Awake() {
        //Limits for bearing hinge joint
        JointLimits bearLim = bearingHj.limits;
        bearLim.min = 0;
        bearLim.max = 0;
        bearingHj.limits = bearLim;
        bearingHj.useLimits = true;
        
        //Limits for elevation hinge joint
        JointLimits elevLim = elevationHj.limits;
        elevLim.min = 0;
        elevLim.max = 0;
        elevationHj.limits = elevLim;
        elevationHj.useLimits = true;

        drive = true;
        neutral = false;
        reverse = false;
        park = false;
        gearType = "D";
        lone = false;

        canMove = true;
        elevLocked = true;

        _line = GetComponent<LineRenderer>();
        _line.enabled = false;

        ammoInd = 0;
        _velocity = 10;

        cameraMovement = GameObject.Find("Bird View Camera").GetComponent<CameraMovement>();
        cameraMovement.birdView = true;

        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        agent.angularSpeed = turnSpeed;
    }

    private void Start() {
        movementAxisName = "Vertical";
        turnAxisName = "Horizontal";
    }

    private void Update() {
        movementInputValue = Input.GetAxis(movementAxisName);
        turnInputValue = Input.GetAxis(turnAxisName);
        SwitchGears();
        Gears();
        AimCannon();
        Fire();
        ResetCannon();
        SwitchHUD();
        ClickToMove();
        if (Input.GetKeyDown(KeyCode.Q)) {
            lone = !lone;
        }
    }

    private void SwitchGears() {
        if (lone) {
            if (Input.GetKeyDown(KeyCode.U)) {
                drive = true;
                neutral = false;
                reverse = false;
                park = false;
                gearType = "D";
            }
            if (Input.GetKeyDown(KeyCode.I)) {
                drive = false;
                neutral = true;
                reverse = false;
                park = false;
                gearType = "N";
            }
            if (Input.GetKeyDown(KeyCode.O)) {
                drive = false;
                neutral = false;
                reverse = true;
                park = false;
                gearType = "R";
            }
            if (!park) {
                if (Input.GetKeyDown(KeyCode.P)) {
                    drive = false;
                    neutral = false;
                    reverse = false;
                    park = true;
                    gearType = "P";
                    changeLimit(0, 60);
                }
            }
        }   
    }

    private void Gears() {
        if (lone) {
            if (drive) {
                Drive();
            }
            if (neutral) {
                Neutral();
            }
            if (reverse) {
                Reverse();
            }
            if (park) {
                Park(); 
            }
        }
        
        
    }

    // Transform tank relative to its facing position (moves forwards or backwards)
    private void Drive() {
        if (canMove && !park && (movementInputValue > 0)) {
            Vector3 movement = transform.forward * movementInputValue * speed * Time.deltaTime;
            bodyRb.MovePosition(bodyRb.position + movement);
            if (movementInputValue != 0) {
                float turn = turnInputValue * turnSpeed * Time.deltaTime;
                Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
                bodyRb.MoveRotation(bodyRb.rotation * turnRotation);
            }
        }
    }

    private void Neutral() {
        if (canMove && !park) {
            float turn = turnInputValue * turnSpeed * Time.deltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            bodyRb.MoveRotation(bodyRb.rotation * turnRotation);
        }
    }

    private void Reverse() {
        if (canMove && !park && (movementInputValue < 0)) {
            Vector3 movement = transform.forward * movementInputValue * speed * Time.deltaTime;
            bodyRb.MovePosition(bodyRb.position + movement);
            if (movementInputValue != 0) {
                float turn = -turnInputValue * turnSpeed * Time.deltaTime;
                Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
                bodyRb.MoveRotation(bodyRb.rotation * turnRotation);
            }
        }
    }
    
    private void Park() {
        canMove = false;
        elevLocked = false;
        bearingHj.useLimits = false;
        cameraMovement.birdView = true;
        _line.enabled = true;
    }

    private void ResetCannon() {
         if (!canMove && !park) {
            // Reverts cannon back to original position through HingeJoint.Limits
            _line.enabled = false;
            float cannonAngle = cannonRb.rotation.eulerAngles.x;
            JointLimits frameLim = elevationHj.limits;
            float frameMax = frameLim.max;
            if (frameMax > 0) {
                float max = frameMax - 1;
                changeLimit(0, max);
            } else {
                elevLocked = true;
                changeLimit(0, 0);
                cameraMovement.birdView = false;
            }

            // Aligns turret angle to tank body angle(back to original position/풀음)
            // Re-enables HingeJoint limits(묶음)
            if (elevLocked) {
                float tankAngle = bodyRb.rotation.eulerAngles.y;
                float turretAngle = turretRb.rotation.eulerAngles.y;
                float deltaAngle = tankAngle - turretAngle;
                if (Mathf.Abs(deltaAngle) > 1) {
                    int hor = 1;
                    if (detHor(tankAngle, deltaAngle)) {
                        hor = -1;
                    }

                    //Rotates turret back to original position via shortest path
                    float resetTurn = hor * turretTurnSpeed  * 2 * Time.deltaTime;
                    Quaternion resetRotation = Quaternion.Euler(0f, resetTurn, 0f);
                    turretRb.MoveRotation(turretRb.rotation * resetRotation);
                } else {
                    bearingHj.useLimits = true;
                    elevationHj.useLimits = true;
                    canMove = true;
                }
            }
        }
    }

    private void AimCannon() {
        if (!canMove && park) {
            //Rotates turret horizontally based on input (bearing)
            float turn = turnInputValue * turretTurnSpeed * Time.deltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            turretRb.MoveRotation(turretRb.rotation * turnRotation);

            //Rotates cannon vertically based on input (elevation)
            float cannonAngle = cannonRb.rotation.eulerAngles.x;
            JointLimits frameLim = elevationHj.limits;
            float frameMin = frameLim.min;
            float min = frameMin + movementInputValue * (turretTurnSpeed / 100);
            if (0 < min && min < 60) {
                changeLimit(min, min + 1);
            }
            
            //Sets up values for direction, angle, velocity, time, while drawing the trajectory of the projectile
            float xrotation = cannonRb.rotation.eulerAngles.x;
            if (xrotation > 90) {
                xrotation = 360 - xrotation;
            }
            changeAmmoType();
            changeCharge();
            _direction = ammoSpawn.right.normalized;
            _angle = roundTenth(xrotation);
            float a = 0.5f * Physics.gravity.y;
            float b = _velocity;
            float c = ammoSpawn.transform.position.y;
            calculateTime(a, b, c);
            drawPath(0.0f);
        }
    }

    private bool detHor(float tankAngle, float deltaAngle) {
        bool firstCond = ((0 <= tankAngle) && (tankAngle < 180)) && ((-180 < deltaAngle) && (deltaAngle < 0));
        bool secondCond = ((180 <= tankAngle) && (tankAngle <= 360)) && !((0< deltaAngle) && (deltaAngle < 180));
        return (firstCond || secondCond);
    }

    private void changeLimit(float min, float max) {
        JointLimits limit = elevationHj.limits;
        limit.min = min;
        limit.max = max;
        elevationHj.limits = limit;
        elevationHj.useLimits = true;
    }

    private void Fire() {
        if (park) {
            if (Input.GetKeyDown(KeyCode.L)) {
                Ammo ammo = Ammo.CreateAmmo(ammoSpawn, cameraMovement.SkyView, ammoInd);
                ammo._velocity = _velocity;
                ammo._direction = _direction;
                ammo._angle = _angle;
                ammo._time = _time;
                ammo._rb.AddForce(_velocity * ammoSpawn.right, ForceMode.Impulse);
                //StartCoroutine(shootProjectile(ammo));
                /*
                _launchParticles.Play();
                _source.PlayOneShot(_clip);
                */
            }
        }  
    }

    public IEnumerator shootProjectile(Ammo ammo) {
        float t = 0;
        ammo._time = Mathf.Max(0.01f, ammo._time);
        while (t < ammo._time && ammo != null) {
            float x = ammo._velocity * t * Mathf.Cos(ammo._angle);
            float y = ammo._velocity * t * Mathf.Sin(ammo._angle) - 0.5f * -Physics.gravity.y * Mathf.Pow(t, 2);
            ammo.transform.position = ammoSpawn.transform.position + ammo._direction * x + Vector3.up * y;
            _blastRadius = ammo.blastRadius;
            t += Time.deltaTime;
            yield return null;
        }  
    }

    private void ClickToMove() {
        if (!lone) {
            if (Input.GetMouseButton(0)) {
                Ray ray = cameraMovement.cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit; 
                if (Physics.Raycast(ray, out hit)) {
                    agent.SetDestination(hit.point);
                }
            }
        }
    }

    private void drawPath(float step) {
        step = Mathf.Max(0.01f, step);
        float lineSegmentCount = _time / step;
        Vector3 blastOrigin = new Vector3(0, 0, 0);
        _linePoints.Clear();
        _linePoints.Add(ammoSpawn.transform.position);
        for (int i = 1; i < lineSegmentCount ; ++i) {
            float stepPassed = step * i;
            float x = _velocity * stepPassed * Mathf.Cos(_angle);
            float y = _velocity * stepPassed * Mathf.Sin(_angle) - 0.5f * -Physics.gravity.y * Mathf.Pow(stepPassed, 2);
            Vector3 newPointOnLine = ammoSpawn.transform.position + _direction * x + Vector3.up * y;
            RaycastHit hit;
            if (Physics.Raycast(_linePoints[i-1], newPointOnLine-_linePoints[i-1], out hit, (newPointOnLine-_linePoints[i-1]).magnitude)) {
                blastOrigin = hit.point;
                _linePoints.Add(blastOrigin);
                break;
            }
            _linePoints.Add(newPointOnLine);
        }
        _line.positionCount = _linePoints.Count;
        _line.SetPositions(_linePoints.ToArray());
    }

    private void SwitchHUD() {
        GameObject notParkUI = canvasUI.transform.GetChild(0).gameObject;
        GameObject parkUI = canvasUI.transform.GetChild(1).gameObject;
        notParkUI.SetActive(!park && canMove);
        parkUI.SetActive(park);
        if (!park && lone) {
            TextMeshProUGUI gearText = notParkUI.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            gearText.text = "Gear: " + gearType;
        } else {
            float bearing = turretRb.rotation.eulerAngles.y / 0.05625f;
            bearing = Mathf.Round(bearing);
            JointLimits cannonLimit = elevationHj.limits;
            float elevation = cannonLimit.min / 0.05625f;
            elevation = Mathf.Round(elevation);
            TextMeshProUGUI bearingText = parkUI.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI elevationText = parkUI.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI ammoTypeText = parkUI.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI chargeText = parkUI.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
            bearingText.text = "Bearing: " + bearing;
            elevationText.text = "Elevation: " + elevation;
            ammoTypeText.text = "Ammo Type: " + IndToTypeName();
            chargeText.text = "Charge: " + chargeUnit;
        }
    }

    private string IndToTypeName() {
        string ret = "Default";
        if (ammoInd == 1) {
            ret = "Timed Shot";
        }
        if (ammoInd == 2) {
            ret = "Triple Bounce";
        }
        return ret;
    }

    private void changeCharge() {
        chargeUnit = (_velocity / 10) + "U";
        if (Input.GetKeyDown(KeyCode.K)) {
            _velocity += 5;
            if (_velocity > 20) {
                _velocity = 10;
            }
        }
    }

    private void changeAmmoType() {
        if (Input.GetKeyDown(KeyCode.J)) {
            ++ammoInd;
            if (ammoInd > 2) {
                ammoInd = 0;
            }
        }
    }

    private float quadraticFormula(float a, float b, float c, float sign) {
        return (-b + sign * Mathf.Sqrt(b * b - 4 * a * c)) / (2 * a);
    }

    private void calculateTime(float a, float b, float c) {
        float tplus = quadraticFormula(a, b, c, 1);
        float tmin = quadraticFormula(a, b, c, -1);
        _time = tplus > tmin ? tplus : tmin;
    }

    private float roundTenth(float value) {
        float ret = value;
        ret = Mathf.Round(ret);
        return ret;
    }
}