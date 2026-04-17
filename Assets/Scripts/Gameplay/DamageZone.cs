using System.Collections;
using UnityEngine;

public class DamageZone : MonoBehaviour
{
    
    public float damagePerSecond = 10f;

   
    public float tickInterval = 0.5f;

 
    public string playerTag = "Player";

    private Coroutine damageCoroutine;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag)) {
            damageCoroutine = StartCoroutine(DoDamageRepeatedly(other.gameObject));
            Debug.Log("Player entered damage zone, starting damage coroutine.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
        }
    }

    private IEnumerator DoDamageRepeatedly(GameObject target)
    {
        var wait = new WaitForSeconds(tickInterval);
        while (true)
        {
            float damageThisTick = damagePerSecond * tickInterval;

            //try to call a typed interface on the target or its parents first.
            IDamageable dmg = null;
            //look for IDamageable on the object or its parents
            dmg = target.GetComponent<IDamageable>();
            if (dmg == null) dmg = target.GetComponentInParent<IDamageable>();
            if (dmg == null) dmg = target.GetComponentInChildren<IDamageable>();

            if (dmg != null)
            {
                //do damage this tick
                dmg.TakeDamage(damageThisTick);
            }

            yield return wait;
        }
    }

    void OnDisable()
    {
        if (damageCoroutine != null)
            StopCoroutine(damageCoroutine);
        damageCoroutine = null;
    }
}
