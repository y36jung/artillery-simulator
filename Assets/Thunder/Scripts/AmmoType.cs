using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoType : MonoBehaviour {

    public static void SetAmmoType(Ammo ammo, int type) {
        int damage = 0;
        float BR = 0f;
        float TOE = 0f;
        float CT = 0f;
        bool DS = true;
        bool TS = false;

        // Default
        if (type == 0) {
            damage = 10;
            BR = 5f;

        // Timed Shot
        } else if (type == 1) {
            damage = 15;
            BR = 12f;
            TOE = 3f;
            DS = false;
            TS = true;
        // Triple Bounce
        } else if (type == 2) {
            damage = 5;
            BR = 10f;
            CT = 3;
            DS = false;
            ammo.physMat.bounciness = 1;
        }
        ammo.dmg = damage;
        ammo.blastRadius = BR;
        ammo.timeOfExp = TOE;
        ammo.collisionTolerance = CT;
        ammo.directShot = DS;
        ammo.timedShot = TS;
    }

}
