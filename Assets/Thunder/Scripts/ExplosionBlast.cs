using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionBlast : MonoBehaviour {
    public GameObject blastObject;
    public int pointsCount;
    public float speedOfExplosion; 
    public float startWidth; 
    public bool damageable = false;

    private float maxRadius;

    private LineRenderer lineRenderer;

    public static ExplosionBlast Create(Vector3 position, float radius) {
        Transform blastPrefab = Instantiate(GameAssets.i.blastPrefab, position, Quaternion.identity);
        ExplosionBlast blast = blastPrefab.GetComponent<ExplosionBlast>();
        blast.maxRadius = radius;
        return blast;
    }


    private void Awake() {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = pointsCount + 1;
    }

    private void Update() {
        StartCoroutine(Blast());
    }

    private IEnumerator Blast() {
        float currentRadius = 0f;
        while (currentRadius < maxRadius) {
            currentRadius += Time.deltaTime * speedOfExplosion;
            Draw(currentRadius);
            yield return null;
        }
        Destroy(blastObject);
    }

    public void Draw(float currentRadius) {
        float angleBetweenPoints = 360f / pointsCount;

        for (int i=0 ; i < pointsCount; ++i) {
            float angle = i * angleBetweenPoints * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f);
            Vector3 position = direction * currentRadius; 

            lineRenderer.SetPosition(i, position);
        }
        lineRenderer.widthMultiplier = Mathf.Lerp(0f, startWidth, 1f - currentRadius / maxRadius);
    }

    public void OnCollisionEnter(Collision col) {

    }
}
