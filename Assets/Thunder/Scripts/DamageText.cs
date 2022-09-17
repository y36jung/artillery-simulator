using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour {
    public bool damageable = false;

    public static DamageText Create(Vector3 position, Vector3 textDirection, int dmg) {
        Transform dmgTextTransform = Instantiate(GameAssets.i.dmgPopupText, position, Quaternion.identity);
        DamageText dmgText = dmgTextTransform.GetComponent<DamageText>();
        dmgText.Setup(dmg);
        Transform dmgTransform = dmgTextTransform.GetComponent<Transform>();
        dmgTransform.rotation = Quaternion.Euler(90, 0, 0);
        return dmgText;
    }

    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;

    private void Awake() {
        textMesh = transform.GetComponent<TextMeshPro>();
    }

    private void Setup(int dmg) {
        textMesh.SetText(dmg.ToString());
        textColor = textMesh.color;
    }

    void Update() {
        float vertSpeed = 20f;
        transform.position += new Vector3(0, vertSpeed) * Time.deltaTime;
        
        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0) {
            float disappearSpeed = 1f;
            textColor.a -= disappearSpeed * Time.deltaTime;
            textMesh.color = textColor;

            if (textColor.a < 0) {
                Destroy(gameObject);
            }
        }
    }
}
