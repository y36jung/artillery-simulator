using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class K9 : PlayableEntities {

    // ***PUBLIC VARIABLES***
    // Rigidbody of Cannon and Turret
    public Rigidbody _rbCannon;
    public Rigidbody _rbTurret;

    // HingeJoint between Body & Turret, Turret & Cannon respectively
    // Used to change bearing of turret and elevation of cannon when aiming
    public HingeJoint _bearingHinge;
    public HingeJoint _elevationHinge;

    public Transform _ammoSpawn;
    public AudioSource _source;
    public AudioClip _clip;
    public ParticleSystem _launchParticles;
    public GameObject _canvasUI;

    // ***PRIVATE VARIABLES***
    // Boolean variables used to check when cannon is locked for movement
    private bool _cannonLocked = true;

    // Unit of charge set when loading ammunition
    private string _charge;
    
    // Used to show the path of projectile before fired from the cannon
    private LineRenderer _projectileLine;
    private List<Vector3> _linePoints = new List<Vector3>();

    // Basic Stats of Ammo Fired
    private float _ammoVelocity;
    private float _ammoBlastRadius;
    private float _angle;
    private float _time;
    private Vector3 _direction;
    

    // Index used to indentify type of ammo used
    private int _ammoIndex;

    // Turn Speed of Turret;
    private float _turretTurnSpeed = 120f;

    // Start is called before the first frame update
    void Start() {
        _entityType = 0;
        _hp = 10;
        _alone = true;

        //Limits for bearing hinge joint
        JointLimits bearingJL = _bearingHinge.limits;
        bearingJL.min = 0;
        bearingJL.max = 0;
        _bearingHinge.limits = bearingJL;
        _bearingHinge.useLimits = true;
        
        //Limits for elevation hinge joint
        JointLimits elevationJL = _elevationHinge.limits;
        elevationJL.min = 0;
        elevationJL.max = 0;
        _elevationHinge.limits = elevationJL;
        _elevationHinge.useLimits = true;

        _projectileLine = GetComponent<LineRenderer>();
        _projectileLine.enabled = false;

        _ammoVelocity = 10;
    }

    // Update is called once per frame
    void Update()
    {
        if (_alone || _park) {
            WASDInput();
        } else {
            ClickToSetDestination();
            MoveToDestination();
        }
        SwitchGearsK9();
        SetGearsK9();
        SwitchHUD();
        AimCannon();
        ResetCannon();
        Fire();
        _agent.enabled = !_alone;

        // Change in values when value of _alone is changed;
        if (Input.GetKeyDown(KeyCode.Q)) {
            _alone = !_alone;
            _cm.birdView = !_alone;
            _moveInputValue = 0;
            _turnInputValue = 0;
            _destination = transform.position;
        }
    }

    // Includes Park gear to SwitchGears()
    private void SwitchGearsK9() {
        if (_entityType == 0) {
            if (Input.GetKeyDown(KeyCode.P)) {
                _park = !_park;
                if (_park) {
                    _drive = false;
                    _neutral = false;
                    _reverse = false;
                    _gear = "P";
                    ChangeLimit(0, 60);
                } else {
                    _drive = true;
                    _neutral = false;
                    _reverse = false;
                    _gear = "D";
                }
            }
        }
        if (_alone) {
            SwitchGears();
        }
        
    }

    // Includes Park gear to SetGears()
    private void SetGearsK9() {
        if (_park) {
            Park();
        } else {
            SetGears();
        }
    }

    // Changes boolean variables from K9 and PlayableEntities for Park Mode
    private void Park() {
        _canMove = false;
        _cannonLocked = false;
        _bearingHinge.useLimits = false;
        _cm.birdView = true;
        _projectileLine.enabled = true;
    }  
 

    private void ResetCannon() {
         if (!_canMove && !_park) {
            // Reverts cannon back to original position through HingeJoint.Limits
            _projectileLine.enabled = false;
            float cannonAngle = _rbCannon.rotation.eulerAngles.x;
            JointLimits frameLim = _elevationHinge.limits;
            float frameMax = frameLim.max;
            if (frameMax > 0) {
                float max = frameMax - 1;
                ChangeLimit(0, max);
            } else {
                _cannonLocked = true;
                ChangeLimit(0, 0);
                if (_alone) {
                    _cm.birdView = false;
                }
            }

            // Aligns turret angle to tank body angle(back to original position/풀음)
            // Re-enables HingeJoint limits(묶음)
            if (_cannonLocked) {
                float tankAngle = _rb.rotation.eulerAngles.y;
                float turretAngle = _rbTurret.rotation.eulerAngles.y;
                float deltaAngle = tankAngle - turretAngle;
                if (Mathf.Abs(deltaAngle) > 1) {
                    int hor = 1;
                    if (DetHor(tankAngle, deltaAngle)) {
                        hor = -1;
                    }

                    //Rotates turret back to original position via shortest path
                    float resetTurn = hor * _turretTurnSpeed  * 2 * Time.deltaTime;
                    Quaternion resetQuaternion = Quaternion.Euler(0f, resetTurn, 0f);
                    _rbTurret.MoveRotation(_rbTurret.rotation * resetQuaternion);
                } else {
                    _bearingHinge.useLimits = true;
                    _elevationHinge.useLimits = true;
                    _canMove = true;
                }
            }
        }
    }

    private void AimCannon() {
        if (!_canMove && _park) {
            //Rotates turret horizontally based on input (bearing)
            float turn = _turnInputValue * _turretTurnSpeed * Time.deltaTime;
            Quaternion turnQuaternion = Quaternion.Euler(0f, turn, 0f);
            _rbTurret.MoveRotation(_rbTurret.rotation * turnQuaternion);

            //Rotates cannon vertically based on input (elevation)
            float cannonAngle = _rbCannon.rotation.eulerAngles.x;
            JointLimits frameLim = _elevationHinge.limits;
            float frameMin = frameLim.min;
            float min = frameMin + _moveInputValue * (_turretTurnSpeed / 100);
            if (0 < min && min < 60) {
                ChangeLimit(min, min + 0.05625f);
            }
            
            //Sets up values for direction, angle, velocity, time, while drawing the trajectory of the projectile
            float xrotation = _rbCannon.rotation.eulerAngles.x;
            if (xrotation > 90) {
                xrotation = 360 - xrotation;
            }
            ChangeAmmoType();
            ChangeCharge();
            _direction = _ammoSpawn.right.normalized;
            _angle = RoundTenth(xrotation);
            float a = 0.5f * Physics.gravity.y;
            float b = _ammoVelocity;
            float c = _ammoSpawn.transform.position.y;
            CalculateTime(a, b, c);
            DrawPath(0.0f);
        }
    }

    private bool DetHor(float tankAngle, float deltaAngle) {
        bool firstCond = ((0 <= tankAngle) && (tankAngle < 180)) && ((-180 < deltaAngle) && (deltaAngle < 0));
        bool secondCond = ((180 <= tankAngle) && (tankAngle <= 360)) && !((0< deltaAngle) && (deltaAngle < 180));
        return (firstCond || secondCond);
    } 

    private void ChangeLimit(float min, float max) {
        JointLimits newJL = _elevationHinge.limits;
        newJL.min = min;
        newJL.max = max;
        _elevationHinge.limits = newJL;
        _elevationHinge.useLimits = true;
    }

    private void Fire() {
        if (_park) {
            if (Input.GetKeyDown(KeyCode.L)) {
                Ammo ammo = Ammo.CreateAmmo(_ammoSpawn, _cm.SkyView, _ammoIndex);
                ammo._velocity = _ammoVelocity;
                ammo._direction = _direction;
                ammo._angle = _angle;
                ammo._time = _time;
                ammo._rb.AddForce(_ammoVelocity * _ammoSpawn.right, ForceMode.Impulse);
                //StartCoroutine(shootProjectile(ammo));
                /*
                _launchParticles.Play();
                _source.PlayOneShot(_clip);
                */
            }
        }  
    }

    // *************************
    public IEnumerator ShootProjectile(Ammo ammo) {
        float t = 0;
        ammo._time = Mathf.Max(0.01f, ammo._time);
        while (t < ammo._time && ammo != null) {
            float x = ammo._velocity * t * Mathf.Cos(ammo._angle);
            float y = ammo._velocity * t * Mathf.Sin(ammo._angle) - 0.5f * -Physics.gravity.y * Mathf.Pow(t, 2);
            ammo.transform.position = _ammoSpawn.transform.position + ammo._direction * x + Vector3.up * y;
            _ammoBlastRadius = ammo.blastRadius;
            t += Time.deltaTime;
            yield return null;
        }  
    }

    private void DrawPath(float step) {
        step = Mathf.Max(0.01f, step);
        float lineSegmentCount = _time / step;
        Vector3 blastOrigin = new Vector3(0, 0, 0);
        _linePoints.Clear();
        _linePoints.Add(_ammoSpawn.transform.position);
        for (int i = 1; i < lineSegmentCount ; ++i) {
            float stepPassed = step * i;
            float x = _ammoVelocity * stepPassed * Mathf.Cos(_angle);
            float y = _ammoVelocity * stepPassed * Mathf.Sin(_angle) - 0.5f * -Physics.gravity.y * Mathf.Pow(stepPassed, 2);
            Vector3 newPointOnLine = _ammoSpawn.transform.position + _direction * x + Vector3.up * y;
            RaycastHit hit;
            if (Physics.Raycast(_linePoints[i-1], newPointOnLine-_linePoints[i-1], out hit, (newPointOnLine-_linePoints[i-1]).magnitude)) {
                blastOrigin = hit.point;
                _linePoints.Add(blastOrigin);
                break;
            }
            _linePoints.Add(newPointOnLine);
        }
        _projectileLine.positionCount = _linePoints.Count;
        _projectileLine.SetPositions(_linePoints.ToArray());
    }

    private void SwitchHUD() {
        GameObject notParkUI = _canvasUI.transform.GetChild(0).gameObject;
        GameObject parkUI = _canvasUI.transform.GetChild(1).gameObject;
        notParkUI.SetActive(!_park && _canMove && _alone);
        parkUI.SetActive(_park);
        if (!_park && _alone) {
            TextMeshProUGUI gearText = notParkUI.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            gearText.text = "Gear: " + _gear;
        } else {
            float bearing = _rbTurret.rotation.eulerAngles.y / 0.05625f;
            bearing = Mathf.Round(bearing);
            JointLimits cannonJL = _elevationHinge.limits;
            float elevation = cannonJL.min / 0.05625f;
            elevation = Mathf.Round(elevation);
            TextMeshProUGUI bearingText = parkUI.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI elevationText = parkUI.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI ammoTypeText = parkUI.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI chargeText = parkUI.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
            bearingText.text = "Bearing: " + bearing;
            elevationText.text = "Elevation: " + elevation;
            ammoTypeText.text = "Ammo Type: " + IndToTypeName();
            chargeText.text = "Charge: " + _charge;
        }
    }

    private string IndToTypeName() {
        string ret = "Default";
        if (_ammoIndex == 1) {
            ret = "Timed Shot";
        }
        if (_ammoIndex == 2) {
            ret = "Triple Bounce";
        }
        return ret;
    }

    private void ChangeCharge() {
        _charge = (_ammoVelocity / 10) + "U";
        if (Input.GetKeyDown(KeyCode.K)) {
            _ammoVelocity += 5;
            if (_ammoVelocity > 20) {
                _ammoVelocity = 10;
            }
        }
    }

    private void ChangeAmmoType() {
        if (Input.GetKeyDown(KeyCode.J)) {
            ++_ammoIndex;
            if (_ammoIndex > 2) {
                _ammoIndex = 0;
            }
        }
    }

    private float QuadraticFormula(float a, float b, float c, float sign) {
        return (-b + sign * Mathf.Sqrt(b * b - 4 * a * c)) / (2 * a);
    }

    private void CalculateTime(float a, float b, float c) {
        float tplus = QuadraticFormula(a, b, c, 1);
        float tmin = QuadraticFormula(a, b, c, -1);
        _time = tplus > tmin ? tplus : tmin;
    }

    private float RoundTenth(float value) {
        float ret = value;
        ret = Mathf.Round(ret);
        return ret;
    }
}
