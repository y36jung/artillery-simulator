using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Ammo : MonoBehaviour {
    /*
    [SerializeField] private AudioSource _source;
    [SerializeField] private AudioClip[] _clips;
    [SerializeField] private GameObject _poofPrefab;
    */
    public int dmg;
    public float blastRadius;
    public float timeOfExp;
    public float collisionTolerance;
    public bool directShot;
    public bool timedShot;
    public bool damageable = false;

    public Rigidbody _rb;
    public PhysicMaterial physMat;
    private Vector3 _position;
    public Vector3 _direction;
    private Transform _firepoint;
    public float _angle;
    public float _time;
    public float _velocity;

    private static Transform textDirection;
    private Coroutine _coroutine;

    public static Ammo CreateAmmo(Transform spawn, Transform cameraPosition, int typeInd) {
        Transform ammoTransform = Instantiate(GameAssets.i.ammoPrefab, spawn.transform.position, Quaternion.identity);
        Ammo ammo = ammoTransform.GetComponent<Ammo>();
        textDirection = cameraPosition;
        AmmoType.SetAmmoType(ammo, typeInd);
        return ammo;
    }

    public void Update() { 
        if (timedShot) {
            timeExplosion();
        }
    } 

    public string topParent(Transform transform) {
        while (transform.parent != null) {
            transform = transform.parent;
        }
        return transform.name;
    }

    public void timeExplosion() {
        timeOfExp -= Time.deltaTime;
        if (timeOfExp <= 0) {
            _position = gameObject.transform.position;
            explode();
        }
    }

    public void explode() {
        ExplosionBlast.Create(_position, blastRadius);
        DamageText.Create(_position, textDirection.position, dmg);
        Destroy(gameObject);
    }

    public void OnCollisionEnter(Collision col) {
        string colParent = topParent(col.collider.transform);
        if (colParent == "Damageable") {
            /*
            Instantiate(_poofPrefab, col.contacts[0].point, Quaternion.Euler(col.contacts[0].normal));
            _source.clip = _clips[Random.Range(0, _clips.Length)];
            _source.Play();
            */
            ContactPoint contact = col.contacts[0];
            Vector3 pos = contact.point;
            _position = pos;
            if (directShot) {
                explode();
            } else if (timedShot) {
                timeExplosion();
            } else {
                --collisionTolerance;
                if (collisionTolerance == 0) {
                    explode();
                }
            }
        }   
    }
}
