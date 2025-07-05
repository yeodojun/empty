using UnityEngine;

[RequireComponent(typeof(HealthUIController), typeof(ManaUIManager))]
public class UIManager : MonoBehaviour
{
    HealthUIController healthCtrl;
    ManaUIManager   manaCtrl;

    [SerializeField] int initialHealth = 5;
    [SerializeField] int initialMana   = 100;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        healthCtrl = GetComponent<HealthUIController>();
        manaCtrl   = GetComponent<ManaUIManager>();
    }

    void Start()
    {
        healthCtrl.maxHealth = initialHealth;
        healthCtrl.InitHearts();

        manaCtrl.SetMana(initialMana);
    }

    public void OnHealthChanged(int newHp) => healthCtrl.UpdateHealthUI(newHp);
    public void OnManaChanged  (int newMana) => manaCtrl.SetMana(newMana);
    public bool TryUseMana     (int cost)   => manaCtrl.Spend(cost);
    public void GainMana       (int amount) => manaCtrl.Gain(amount);
}
