kullandığım versiyon -> 6000.4.0a2
Sahneyi başlatmak için, ThirdPerson->scenes içindeki PlayGround sahnesini başlatın.

Canavar saldırı mekanizması:
Sistem, iki koşulun aynı anda doğru olmasını bekliyor:
1) Doğru Zamanlama: Canavar, saldırı animasyonunun "hasar veren penceresi" içinde mi? (Sizin ayarladığınız 20. ve 30. saniyeler, yani AnimEvent_StartDamageWindow ile AnimEvent_EndDamageWindow arası).)
2) Doğru Mesafe: Ana karakter, canavarın attackRange'i içinde mi?
