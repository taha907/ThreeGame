using UnityEngine;
using UnityEngine.UI; // UI elemanlarını kullanmak için bu satır GEREKLİ
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Can Ayarları")]
    public int maxHealth = 100; // Başlangıçtaki maksimum can
    public int currentHealth;   // Mevcut canımız

    [Header("UI Elemanları")]
    [Tooltip("Canın yazılacağı Text elemanını buraya sürükleyin")]
    public Text healthText; // Canı gösterecek metin

    // Diğer (isteğe bağlı)
    public bool isDead = false;

    void Start()
    {
        // Oyuna başlarken canı doldur
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    /// <summary>
    /// Bu fonksiyon, canavar gibi diğer scriptler tarafından çağrılacak.
    /// </summary>
    /// <param name="damageAmount">Alınan hasar miktarı</param>
    public void TakeDamage(int damageAmount)
    {
        // Eğer zaten öldüysek, tekrar hasar alma
        if (isDead) return;

        // Canı azalt
        currentHealth -= damageAmount;

        // Canın 0'ın altına düşmesini engelle
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        // Konsola ve UI'a bilgi ver
        Debug.LogWarning("Oyuncu " + damageAmount + " hasar aldı. Kalan can: " + currentHealth);
        UpdateHealthUI();

        // Can 0'a ulaştı mı kontrol et
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// UI Text elemanını günceller
    /// </summary>
    void UpdateHealthUI()
    {
        if (healthText != null)
        {
            // Text elemanının metnini güncelle
            healthText.text = "CAN: " + currentHealth;
        }
        else
        {
            // Bu, 'healthText'i atamayı unutursanız sizi uyarır
            Debug.LogError("Health Text (Can Metni) atanmamış! - PlayerHealth.cs");
        }
    }

    /// <summary>
    /// Oyuncu öldüğünde çalışır
    /// </summary>
    void Die()
    {
        isDead = true;
        Debug.LogError("OYUNCU ÖLDÜ!");

        // --- Buraya ölüm animasyonu veya oyunu yeniden başlatma kodu ekleyebilirsiniz ---
        
        // Şimdilik, oyuncuyu pasif hale getirelim
        // gameObject.SetActive(false); 
    }
}