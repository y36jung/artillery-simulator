using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayableEntities : MonoBehaviour {

    // ***PUBLIC VARIABLES***
    public Rigidbody _rb;
    public bool _damageable;

    // if _entitytype == 0 => K9
    //    _entitytype == 1 => K77
    //    _entitytype == 2 => K10
    public int _entityType;

    // Basic Tank Stat Variables
    public float _hp;
    public float _typeOfMovement;
    public float _moveSpeed;
    public float _turnSpeed;

    // Boolean Variables that determine WASD Movement
    public bool _canMove;
    public bool _drive;
    public bool _neutral;
    public bool _reverse;
    public bool _park;
    public bool _alone;

    // Variables that determine current gear during WASD Movement
    public string _gear;

    public CameraMovement _cm;

    // WASD Movement Variables
    public float _moveInputValue;
    public float _turnInputValue;
    
    // Variables that are used for Camera movement when using WASD Movement
    public Camera _camera;

    // Variables used for movement via MouseButton
    public NavMeshAgent _agent;
    public Vector3 _destination;
    public bool _atDestination;
    public bool _selectedEntity;

    public void Awake() {
        _moveSpeed = 20f;
        _turnSpeed = 120f;
        
        _canMove = true;
        _drive = true;
        _neutral = false;
        _reverse = false;
        _park = false;

        _gear = "D";
        
        _camera = _cm.cam;

        _agent.speed = _moveSpeed * 5;
        _agent.angularSpeed = _turnSpeed;
        _agent.acceleration = 20;

        _selectedEntity = false;
    }

    // ***MOUSE MOVEMENT****
    public void ClickToSetDestination() {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        // Left-click to select entity that player intends to move
        if (Input.GetMouseButton(0)) {
            if (Physics.Raycast(ray, out hit)) {
                if (hit.transform != null) {
                    string topName = topParent(hit.transform);
                    string name = hit.transform.parent.name;
                    Debug.Log(name);
                    int entityType = -1;
                    // Identify entity that was clicked via name of Transform
                    if (topName == "TankEntities") {
                        if (name == "K9 Main") {
                            entityType = 0;
                        }
                        if (entityType == _entityType) {
                            _selectedEntity = true;
                        } else {
                            _selectedEntity = false;
                        }
                    }
                }
            }
        }
        // Right-click a point on platform to move (Limited by NavMesh)
        if (Input.GetMouseButton(1) && _selectedEntity) {
            if (Physics.Raycast(ray, out hit)) {
                _destination = hit.point;
            }
        }
    }

    // Explanation: Used to use NavMeshAgent.SetDestination() and use the Unity API, but didn't move as intended.
    //              Instead, movement was manually programmed by translating Transform a distance over time.
    public void MoveToDestination() {
        float distance = Vector3.Distance(transform.position, _destination);
        if (distance > 1.0) {
            float snapTime = Time.deltaTime * 5;
            Vector3 direction = _destination - transform.position;
            var rotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, snapTime);
            float angleDifference = Quaternion.Angle(transform.rotation, rotation);
            if (angleDifference < 180 && distance > 3.0) {
                Vector3 movement = transform.forward * Time.deltaTime * _moveSpeed;
                _agent.Move(movement);
            }
        }
    }

    public void WASDToMove() {
        WASDInput();
        SwitchGears();
        SetGears();
    }
    
    // ***WASD MOVEMENT***
    // Sets values of movement variables
    public void WASDInput() {
        _moveInputValue = Input.GetAxis("Vertical");
        _turnInputValue = Input.GetAxis("Horizontal");
    }

    // Switches boolean variables for gears during WASD Movement
    public void SwitchGears() {
        if (Input.GetKeyDown(KeyCode.U)) {
            _drive = true;
            _neutral = false;
            _reverse = false;
            _park = false;
            _gear = "D";
        }
        if (Input.GetKeyDown(KeyCode.I)) {
            _drive = false;
            _neutral = true;
            _reverse = false;
            _park = false;
            _gear = "N";
        }
        if (Input.GetKeyDown(KeyCode.O)) {
            _drive = false;
            _neutral = false;
            _reverse = true;
            _park = false;
            _gear = "R";
        }
    }

    // Sets gear based on values of boolean variables
    public void SetGears() {
        if (_drive) {
            Drive();
        }
        if (_neutral) {
            Neutral();
        }
        if (_reverse) {
            Reverse();
        }
    }

    public void Drive() {
        if (_canMove && !_park && (_moveInputValue > 0)) {
            Vector3 moveVector3 = transform.forward * _moveInputValue * _moveSpeed * Time.deltaTime;
            _rb.MovePosition(_rb.position + moveVector3);
            if (_moveInputValue != 0) {
                float turn = _turnInputValue * _turnSpeed * Time.deltaTime;
                Quaternion turnQuaternion = Quaternion.Euler(0f, turn, 0f);
                _rb.MoveRotation(_rb.rotation * turnQuaternion);
            }
        }
    }

    public void Neutral() {
        if (_canMove && !_park) {
            float turn = _turnInputValue * _turnSpeed * Time.deltaTime;
            Quaternion turnQuaternion = Quaternion.Euler(0f, turn, 0f);
            _rb.MoveRotation(_rb.rotation * turnQuaternion);
        }
    }

    public void Reverse() {
        if (_canMove && !_park && (_moveInputValue < 0)) {
            Vector3 move = transform.forward * _moveInputValue * _moveSpeed * Time.deltaTime;
            _rb.MovePosition(_rb.position + move);
            if (_moveInputValue != 0) {
                float turn = _turnInputValue * _turnSpeed * -1 * Time.deltaTime;
                Quaternion turnQuaternion = Quaternion.Euler(0f, turn, 0f);
                _rb.MoveRotation(_rb.rotation * turnQuaternion);
            }
        }
    }

    public string topParent(Transform transform) {
        while (transform.parent != null) {
            transform = transform.parent;
        }
        return transform.name;
    }
}