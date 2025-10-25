using UnityEngine;
using UnityEngine.AI; // NavMeshAgent için bu satır gerekli
using System.Collections; 
using System.Collections.Generic; // List kullanmak için bu gerekli

public class CanavarScripts : MonoBehaviour
{
    // ... (Çoğu değişken aynı) ...
    private NavMeshAgent agent;
    private Animator animator;
    public Transform player;
    private enum State { Patrolling, Chasing, Searching, Attacking }
    private State currentState;
    
    [Header("Patrol Area Settings")]
    public Transform patrolCenter; 
    public Vector2 patrolAreaSize = new Vector2(20f, 20f);
    public float searchNearbyRadius = 10f; 
    private Vector3 startPosition; 
    public float patrolWaitTime = 4.0f;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    [Header("Chase and Attack Settings")]
    public float sightRange = 10f;
    public float stopChasingRange = 15f;
    public float chaseSpeed = 4.8f;
    public float walkSpeed = 2.0f;
    [Tooltip("Saldırı animasyonunuzun süresiyle (veya biraz fazlasıyla) aynı olmalı")]
    public float attackCooldown = 1.5f; 
    [Tooltip("Oyuncuya saldırı yapacağı mesafe (dibine gelme)")]
    public float attackRange = 2.0f; 
    private float attackTimer = 0f; 
    public float searchWaitTime = 3.0f; 

    private int animIDSpeed;
    private int animIDAttack; 

    // --- YENİ DEĞİŞKENLER ---
    [Header("Attack Hit Check")]
    private bool isCheckingDamage = false; // Hasar penceresi açık mı?
    private List<Transform> hitList = new List<Transform>(); // Bu vuruşta hasar alanların listesi
    // --- BİTTİ ---

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
        animIDAttack = Animator.StringToHash("Attack"); 
        agent.speed = walkSpeed; 
        agent.isStopped = true;
        isWaiting = true;
        waitTimer = patrolWaitTime;
        currentState = State.Patrolling;
    }


    void Update()
    {
        if (player == null || !agent.isOnNavMesh) return;
        
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }

        // --- YENİ: VURUŞ PENCERESİ KONTROLÜ ---
        // Eğer 'isCheckingDamage' anahtarı açıksa (Animasyon Olayı ile açıldıysa),
        // her frame hasar kontrolü yap.
        if (isCheckingDamage)
        {
            ActiveDamageCheck();
        }
        // --- BİTTİ ---

        switch (currentState)
        {
            case State.Patrolling:
                HandlePatrolling();
                break;
            case State.Chasing:
                HandleChasing();
                break;
            case State.Searching:
                HandleSearching();
                break;
            case State.Attacking:
                HandleAttacking();
                break;
        }
        UpdateAnimator();
    }

    private void SetState(State newState)
    {
        if (currentState == newState) return;

        // --- YENİ: DURUM DEĞİŞİMİ GÜVENLİĞİ ---
        // Eğer saldırı durumu herhangi bir sebeple kesilirse,
        // hasar penceresini kapattığımızdan emin ol.
        if (currentState == State.Attacking && newState != State.Attacking)
        {
            isCheckingDamage = false;
        }
        // --- BİTTİ ---

        currentState = newState;

        switch (currentState)
        {
            case State.Patrolling:
                Debug.Log("NPC: Devriyeye dönüyorum (YÜRÜYEREK).");
                agent.speed = walkSpeed;
                agent.isStopped = false;
                isWaiting = false;
                waitTimer = 0f;
                SearchNewPatrolPoint(); 
                break;
            case State.Chasing:
                Debug.Log("NPC: Oyuncuyu gördüm! Kovalıyorum (KOŞARAK)!");
                agent.speed = chaseSpeed;
                agent.isStopped = false; 
                isWaiting = false;
                waitTimer = 0f;
                break;
            case State.Searching:
                Debug.Log("NPC: Oyuncuyu kaybettim/Saldırı bitti. Duraksıyorum...");
                agent.speed = walkSpeed; 
                agent.isStopped = true;  
                isWaiting = true;
                waitTimer = searchWaitTime; 
                break;
            case State.Attacking:
                Debug.Log("NPC: Oyuncu menzilde! Saldırıyorum!");
                agent.speed = 0; 
                agent.isStopped = true; 
                agent.ResetPath(); 
                isWaiting = false;
                waitTimer = 0f;
                attackTimer = 0f; 
                break;
        }
    }

    private void HandlePatrolling()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer < attackRange) { SetState(State.Attacking); return; }
        if (distanceToPlayer < sightRange) { SetState(State.Chasing); return; }
        if (isWaiting)
        {
            if (!agent.isStopped) agent.isStopped = true;
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                agent.isStopped = false;
                SearchNewPatrolPoint();
            }
        }
        else
        {
            if (agent.isStopped) agent.isStopped = false;
            if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance)
            {
                isWaiting = true;
                waitTimer = patrolWaitTime;
                agent.isStopped = true;
            }
        }
    }

    private void HandleChasing()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer < attackRange) { SetState(State.Attacking); return; }
        if (distanceToPlayer > stopChasingRange) { SetState(State.Searching); return; }
        if (agent.isStopped) agent.isStopped = false; 
        agent.SetDestination(player.position);
    }

    private void HandleSearching()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer < attackRange) { SetState(State.Attacking); return; }
        if (distanceToPlayer < sightRange)
        {
            SetState(State.Chasing);
            return;
        }
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                agent.isStopped = false;
                if (distanceToPlayer < sightRange)
                {
                    SetState(State.Chasing);
                }
                else if (Random.Range(0f, 1f) < 0.5f)
                {
                    Debug.Log("NPC: (Karar A) Normal devriyeye dönüyorum.");
                    SetState(State.Patrolling); 
                }
                else
                {
                    Debug.Log("NPC: (Karar B) Şu yakına bir bakayım.");
                    SearchNearbyPoint(); 
                    currentState = State.Patrolling;
                }
            }
        }
    }

    private void HandleAttacking()
    {
        if (!agent.isStopped) 
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        agent.velocity = Vector3.zero; 
        FaceTarget(player.position);

        if (attackTimer > 0f)
        {
            return; 
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer < attackRange)
        {
            animator.SetTrigger(animIDAttack);
            Debug.Log("NPC: SALDIRI animasyonu BAŞLADI!");
            attackTimer = attackCooldown; 
        }
        else
        {
            SetState(State.Searching);
        }
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 lookDirection = (target - transform.position).normalized;
        lookDirection.y = 0; 
        if (lookDirection != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f); 
        }
    }
    
    private void SearchNewPatrolPoint()
    {
        Vector3 center = (patrolCenter != null) ? patrolCenter.position : startPosition;
        float randomX = Random.Range(-patrolAreaSize.x / 2, patrolAreaSize.x / 2);
        float randomZ = Random.Range(-patrolAreaSize.y / 2, patrolAreaSize.y / 2); 
        Vector3 randomPoint = new Vector3(center.x + randomX, center.y, center.z + randomZ);
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 5.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            isWaiting = true; 
            waitTimer = patrolWaitTime;
            agent.isStopped = true;
        }
    }

    private void SearchNearbyPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * searchNearbyRadius; 
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, searchNearbyRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            SetState(State.Patrolling);
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        float currentSpeed = 0f;
        if (!agent.isStopped)
        {
            currentSpeed = agent.velocity.magnitude;
        }
        animator.SetFloat(animIDSpeed, currentSpeed, 0.1f, Time.deltaTime);
    }
    public void OnFootstep() { }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; 
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red; 
        Gizmos.DrawWireSphere(transform.position, stopChasingRange);
        Gizmos.color = Color.magenta; 
        Gizmos.DrawWireSphere(transform.position, attackRange); 
        Gizmos.color = Color.green;
        Vector3 center = (patrolCenter != null) ? patrolCenter.position : (Application.isPlaying ? startPosition : transform.position);
        Vector3 size = new Vector3(patrolAreaSize.x, 2.0f, patrolAreaSize.y); 
        Gizmos.DrawWireCube(center, size);
    }

    
    // ------------------------------------------------------------------
    // *** TAMAMEN YENİ FONKSİYONLAR (VURUŞ PENCERESİ VE HASAR İÇİN) ***
    // ------------------------------------------------------------------

    /// <summary>
    /// Update() içinde her frame çağrılır, AMA SADECE vuruş penceresi aktifken.
    /// </summary>
    private void ActiveDamageCheck()
    {
        if (player == null) return;
                
        // 1. Oyuncu zaten bu vuruşta hasar aldı mı diye kontrol et.
        if (hitList.Contains(player.transform))
        {
            return; // Aldıysa, tekrar vurma.
        }

        // 2. Oyuncu menzilde mi diye kontrol et.
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer < attackRange)
        {
            // BAŞARILI VURUŞ!
            Debug.LogWarning("ANA KARAKTERE VURDUM! (Savurma Sırasında)");
            hitList.Add(player.transform); // Bu vuruş için listeye ekle, tekrar vurmasın.
            
            // --- HASAR VERME KISMI ---
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(10); // 10 hasar ver
            }
            else
            {
                Debug.LogError("Canavar: 'Player' objesinde 'PlayerHealth' script'i bulunamadı!");
            }
            // --- BİTTİ ---
        }
    }

    /// <summary>
    /// Bu fonksiyon Animasyon Olayı (Animation Event) tarafından çağrılacak.
    /// Vuruşun BAŞLADIĞI an.
    /// </summary>
    public void AnimEvent_StartDamageWindow()
    {
        Debug.Log("Hasar Penceresi AÇILDI");
        isCheckingDamage = true;
        hitList.Clear(); // Yeni vuruş için "kimler hasar aldı" listesini temizle
    }

    /// <summary>
    /// Bu fonksiyon Animasyon Olayı (Animation Event) tarafından çağrılacak.
    /// Vuruşun BİTTİĞİ an.
    /// </summary>
    public void AnimEvent_EndDamageWindow()
    {
        Debug.Log("Hasar Penceresi KAPANDI");
        isCheckingDamage = false;
    }
}