using UnityEngine;
using UnityEngine.AI; // NavMeshAgent için bu satır gerekli
using System.Collections; // Gerekli değil ama iyi bir alışkanlık

public class NpcAIController : MonoBehaviour
{
    // Gerekli Bileşenler
    private NavMeshAgent agent;
    private Animator animator;

    // Hedef (Oyuncu)
    public Transform player;

    // *** YENİ: Durumlar (State Machine) güncellendi ***
    private enum State 
    { 
        Patrolling,  // Normal devriye (yürü, bekle, yürü)
        Chasing,     // Oyuncuyu kovala (koş)
        Searching    // Oyuncuyu yeni kaybettim, duraksıyorum (dur, sonra karar ver)
    }
    private State currentState;

    // Devriye Ayarları
    [Tooltip("NPC'nin başlangıç noktasından ne kadar uzağa devriye atacağı")]
    public float patrolRange = 10f;
    private Vector3 startPosition;
    
    [Tooltip("NPC'nin devriye noktasına ulaştığında kaç saniye bekleyeceği")]
    public float patrolWaitTime = 4.0f; 
    private float waitTimer = 0f; 
    private bool isWaiting = false; 

    // Kovalama Ayarları
    [Tooltip("Oyuncuyu görme mesafesi")]
    public float sightRange = 10f;
    [Tooltip("Oyuncuyu kovalamayı bırakacağı mesafe")]
    public float stopChasingRange = 15f; 
    [Tooltip("Ana karakterin SprintSpeed değeriyle aynı olmalı")]
    public float chaseSpeed = 4.8f; 
    [Tooltip("Ana karakterin MoveSpeed değeriyle aynı olmalı")]
    public float walkSpeed = 2.0f; 

    // *** YENİ: Duraksama (Search) süresi ***
    [Tooltip("Oyuncuyu kaybedince kaç saniye duraksayacağı")]
    public float searchWaitTime = 3.0f; // 3 saniye duraksama

    // Animator Parametre ID'si
    private int animIDSpeed;

    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        agent.updatePosition = true;
        agent.updateRotation = true;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
            else Debug.LogError("NPC: 'Player' tag'ine sahip bir oyuncu bulunamadı!");
        }
        
        startPosition = transform.position;
        animIDSpeed = Animator.StringToHash("Speed");

        // Başlangıç durumu: Devriye (bekleyerek başla)
        agent.speed = walkSpeed; // Hızı "yürüme" olarak ayarla
        agent.isStopped = true;  
        isWaiting = true;        
        waitTimer = patrolWaitTime; 
        currentState = State.Patrolling; 
    }

    void Update()
    {
        if (player == null || !agent.isOnNavMesh) return;

        // Durum makinesini çalıştır
        switch (currentState)
        {
            case State.Patrolling:
                HandlePatrolling();
                break;
            case State.Chasing:
                HandleChasing();
                break;
            // *** YENİ: Searching durumunu işle ***
            case State.Searching:
                HandleSearching();
                break;
        }

        UpdateAnimator();
    }

    private void SetState(State newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        
        switch (currentState)
        {
            case State.Patrolling:
                Debug.Log("NPC: Devriyeye dönüyorum (YÜRÜYEREK).");
                agent.speed = walkSpeed;   // 1. İSTEK: Yürüme hızı
                agent.isStopped = false; 
                isWaiting = false;       
                waitTimer = 0f;
                // Patrolling durumu artık kendi içinde nokta arayacak
                SearchNewPatrolPoint(); 
                break;

            case State.Chasing:
                Debug.Log("NPC: Oyuncuyu gördüm! Kovalıyorum (KOŞARAK)!");
                agent.speed = chaseSpeed;
                agent.isStopped = false; 
                isWaiting = false;       
                waitTimer = 0f;
                break;
            
            // *** YENİ: Searching Durumu ***
            case State.Searching:
                Debug.Log("NPC: Oyuncuyu kaybettim. Duraksıyorum...");
                agent.speed = walkSpeed;   // 1. İSTEK: Hızı yürüme yap (dursa bile)
                agent.isStopped = true;    // 2. İSTEK: Duraksamayı başlat
                isWaiting = true;          // Bekleme sayacını kullan
                waitTimer = searchWaitTime; // Duraksama süresini ayarla
                break;
        }
    }

    private void HandlePatrolling()
    {
        // 1. ÖNCELİK: Oyuncu menzile girerse kovala
        if (Vector3.Distance(transform.position, player.position) < sightRange)
        {
            SetState(State.Chasing);
            return;
        }

        // 2. Eğer bekleme durumundaysak (ya hedefe ulaştık ya da yeni başladık)
        if (isWaiting)
        {
            if (!agent.isStopped) agent.isStopped = true;

            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                // Bekleme bitti, yeni bir nokta ara ve yürümeye başla
                isWaiting = false;
                agent.isStopped = false; 
                SearchNewPatrolPoint(); // Ana devriye rotasından nokta bul
            }
        }
        // 3. Eğer bekleme durumunda DEĞİLSEK (yani yürüyorsak)...
        else
        {
            if (agent.isStopped) agent.isStopped = false; 

            // Hedefe ulaşıp ulaşmadığımızı kontrol et
            if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance)
            {
                // Hedefe ulaştık, beklemeye başla
                isWaiting = true;
                waitTimer = patrolWaitTime;
                agent.isStopped = true;
                Debug.Log("NPC: Devriye noktasına ulaştım. Bekliyorum.");
            }
        }
    }

    private void HandleChasing()
    {
        // Oyuncu menzilden çıkarsa DEVRIYE yerine ARAMA moduna geç
        if (Vector3.Distance(transform.position, player.position) > stopChasingRange)
        {
            // *** GÜNCELLEME: Artık Patrolling'e değil, Searching'e gidiyor ***
            SetState(State.Searching); 
            return;
        }

        // Oyuncuyu takip et (koşarak)
        agent.SetDestination(player.position);
    }

    // *** YENİ: Duraksama ve Karar Verme Fonksiyonu ***
    private void HandleSearching()
    {
        // 1. ÖNCELİK: Oyuncu tekrar menzile girerse kovala
        if (Vector3.Distance(transform.position, player.position) < sightRange)
        {
            SetState(State.Chasing);
            return;
        }

        // 2. Duraksama sayacını çalıştır
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            
            // 3. Duraksama bittiyse karar ver
            if (waitTimer <= 0)
            {
                isWaiting = false;
                agent.isStopped = false;

                // 3. İSTEK: %50 şansla karar ver
                if (Random.Range(0f, 1f) < 0.5f)
                {
                    // Seçenek A: Ana devriye rotasına dön (startPosition etrafında)
                    Debug.Log("NPC: (Karar A) Normal devriyeye dönüyorum.");
                    SetState(State.Patrolling); // Bu, SearchNewPatrolPoint()'u tetikler
                }
                else
                {
                    // Seçenek B: "Random devam et" (Bulunduğu yerin etrafında)
                    Debug.Log("NPC: (Karar B) Şu yakına bir bakayım.");
                    SearchNearbyPoint(); // Yakında bir nokta bul
                    
                    // State'i manuel olarak Patrolling yapıyoruz ki
                    // SetState'in içindeki SearchNewPatrolPoint'u tekrar çağırmasın.
                    currentState = State.Patrolling; 
                }
            }
        }
    }

    // Devriye (Ana rotaya dön)
    private void SearchNewPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRange;
        randomDirection += startPosition; // Merke: Başlangıç noktası

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRange, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            Debug.Log("NPC: Yeni ana devriye noktasına gidiyorum: " + hit.position);
        }
        else
        {
            // Bulamazsa beklesin (HandlePatrolling bunu yönetecek)
            isWaiting = true; 
            waitTimer = patrolWaitTime;
            agent.isStopped = true;
        }
    }

    // *** YENİ: Yakında bir nokta ara (Random devam et) ***
    private void SearchNearbyPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRange; // Aynı menzili kullanabiliriz
        randomDirection += transform.position; // *** FARK: Merke: Mevcut konum ***

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRange, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            Debug.Log("NPC: Yakındaki rastgele noktaya gidiyorum: " + hit.position);
        }
        else
        {
            // Bulamazsa ana devriyeye dönsün
            SetState(State.Patrolling);
        }
    }


    private void UpdateAnimator()
    {
        if (animator == null) return;
        float currentSpeed = agent.velocity.magnitude;
        animator.SetFloat(animIDSpeed, currentSpeed, 0.1f, Time.deltaTime);
    }
    
    public void OnFootstep()
    {
        // Ayak sesi
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopChasingRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(startPosition, patrolRange);
    }
}