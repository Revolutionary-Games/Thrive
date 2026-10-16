Achievements
============

Achievement definitions live in `simulation_parameters/common/achievements.json`.
`FileLoadedAchievement` checks their linked statistic against the threshold.
Gameplay reports events through `AchievementEvents`; `AchievementsManager` applies
cheat/freebuild eligibility, locks statistic updates, and forwards unlocks to the UI
and Steam. `AchievementStatStore` persists offline statistics; `SteamStatStore`
uses the names in `IAchievementStatStore`.

