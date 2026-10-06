# Chronos Repair Shop (Zaman Bükücü Saatçi)

Fizik tabanlı bulmaca + arcade mobil oyun prototipi. Unity **6.3 LTS (6000.3)**, 2D (Physics2D), legacy Input Manager + uGUI.

## Oyun döngüsü

```
Placement ──[Başlat veya süre dolunca]──► Running ──► Won  (kapı açıkken top kapıya girdi)
   │ parçaları sürükle/döndür               │ top bırakılır / ateşlenir └► Failed (Zaman Sızıntısı / Sıkışma / Karanlık)
   └ opsiyonel yerleştirme süresi           └ karanlık sayacı işler
```

| Mekanik | Sınıf |
|---|---|
| Bölüm akışı, süreler, yıldız, era fiziği (yerçekimi, rüzgâr, sürtünme, yerçekimi ters çevirme) | `Core/LevelController` |
| Enerji topu, sıkışma tespiti | `Mechanics/EnergyBall` |
| Dişli: topla tahrik, komşu dişliye geçiş (dişleri tutan ama sıkışmayan mesafe), gerçek dönüş | `Mechanics/Gear` |
| Ana mekanizma: tüm gerekli dişliler dönünce kapıyı açar | `Mechanics/ClockMechanism`, `ExitGate` |
| Yay (yönü parçanın açısı belirler) | `Mechanics/SpringBouncer` |
| Ayna (açı-giriş = açı-çıkış) / rampa | `Mechanics/Deflector` |
| Dışarı düşme = Zaman Sızıntısı | `Mechanics/LeakZone` |
| Sürükle-bırak, 2 parmak döndürme, ızgara snap, geçerlilik kontrolü | `Placement/*` |
| Bölüm / era / kampanya verisi (ScriptableObject), ilerleme kaydı, hikâye parçaları | `Data/*`, `Core/SaveSystem` |

Doğruluk testi: parça açısı yanlışsa top aynadan başka yere gider / yaya yanlış yönden çarpar; dişliler birbirine
değmiyorsa güç geçmez; top kapalı kapıda veya yuvada durursa `Stuck`; düşerse `TimeLeak`.

## Klasör yapısı

```
Assets/
├─ Resources/Campaign.asset          (CampaignData - siz oluşturursunuz)
└─ _Project/
   ├─ Scripts/{Core,Data,Mechanics,Placement,UI}
   ├─ Scenes/        MainMenu + her bölüm için bir sahne (Level_Egypt_01 ...)
   ├─ Prefabs/{Parts,Level,UI}
   ├─ ScriptableObjects/{Parts,Levels,Eras}
   ├─ Physics/       PhysicsMaterial2D (Bouncy, Slippery, Sand)
   └─ Art/ Audio/
```

## Editörde kurulum (bir kerelik)

**Katmanlar:** `Ball`, `Parts`, `Gears`, `Static`, `PlacementZone`, `Leak`.
Physics2D Layer Collision Matrix: `Ball` ↔ `Parts/Gears/Static` açık; `Parts/Gears` kendi aralarında kapalı; `PlacementZone` ve `Leak` sadece `Ball` ile (trigger).

**Parça prefab'ları** (`PlaceablePart` kök bileşen, layer `Parts`):
- Dişli: `CircleCollider2D` (dış çap = diş ucu) + `Gear` — Rigidbody2D Kinematic, Sürtünmeli PhysicsMaterial2D
- Yay: `BoxCollider2D` (pad yüzü +Y) + `SpringBouncer`
- Ağırlık: `Rigidbody2D` **Dynamic** (yerleştirmede otomatik Kinematic, başlatınca Dynamic olur)
- Ayna/Rampa: `BoxCollider2D` + `Deflector` (Mode: Mirror / Ramp)

**Top prefab'ı:** `EnergyBall`, `Rigidbody2D` Dynamic, `CircleCollider2D` (Bouncy mat.), layer `Ball`.

**Bölüm sahnesi:** `LevelController` + `PlacementController` (+ `Main Camera`, `EventSystem`), `PlacementZone` kutuları,
sahneye sabit gerekli dişliler + `ClockMechanism` + `ExitGate` (kapalıyken kapıyı tutan solid `door` collider'ı + trigger), `LeakZone`'lar,
Canvas altında `HudController`, `ResultPanel`, parça çubuğu (`PartSlotUI` prefab'ı).
Sahne tek başına Play edilebilir: `LevelController.fallbackLevel` alanına `LevelData` atayın.

**Veri:** `Create > Chronos > Part Definition / Level Data / Era Data / Campaign`. Campaign asset'i `Assets/Resources/Campaign` olmalı.

## Era başına fizik kuralları (`LevelData.rules`)

- Antik Mısır kum saati: `ballDrag` yüksek, `wind` ile akan kum.
- Rönesans dişli saat: standart fizik, çok dişli zincirleri.
- Kuantum saat: `gravityFlipInterval` > 0 — yerçekimi periyodik ters döner.

## Sıradaki adımlar

1. İlk bölüm sahnesi (Egypt_01: 1 gerekli dişli, 2 dişli + 1 ayna) ve placeholder sprite'lar.
2. Başarısızlık/başarı juice'u: parçacıklar, ses, ekran sarsıntısı, kapı animasyonu.
3. Yerleştirme önizlemesi: topun yörünge tahmini (hayalet simülasyon).
4. Ana menü + era seçimi + zaman çizgisi onarım görselleri (`EraData.RepairedFraction`).
5. EditMode testleri (`PartInventory`, `CampaignData.GetNext`, `SaveSystem`).
