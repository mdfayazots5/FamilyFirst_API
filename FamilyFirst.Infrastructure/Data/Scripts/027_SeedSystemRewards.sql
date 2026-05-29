IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Rewards
    WHERE RewardName = N'Extra 15 Minutes Screen Time'
        AND FamilyId IS NULL
        AND IsSystem = 1
)
BEGIN
    INSERT INTO dbo.Rewards (RewardName, Description, IconCode, Category, CoinCost, IsSystem, IsEnabled)
    VALUES (N'Extra 15 Minutes Screen Time', N'Unlock 15 extra minutes of screen time.', N'screen_15m', N'ScreenTime', 50, 1, 1);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Rewards
    WHERE RewardName = N'Choose Movie Night'
        AND FamilyId IS NULL
        AND IsSystem = 1
)
BEGIN
    INSERT INTO dbo.Rewards (RewardName, Description, IconCode, Category, CoinCost, IsSystem, IsEnabled)
    VALUES (N'Choose Movie Night', N'Pick the family movie for the next movie night.', N'movie_night', N'ScreenTime', 120, 1, 1);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Rewards
    WHERE RewardName = N'Ice Cream Treat'
        AND FamilyId IS NULL
        AND IsSystem = 1
)
BEGIN
    INSERT INTO dbo.Rewards (RewardName, Description, IconCode, Category, CoinCost, IsSystem, IsEnabled)
    VALUES (N'Ice Cream Treat', N'Enjoy a favorite ice cream treat.', N'ice_cream', N'FoodTreat', 80, 1, 1);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Rewards
    WHERE RewardName = N'Favorite Snack Pick'
        AND FamilyId IS NULL
        AND IsSystem = 1
)
BEGIN
    INSERT INTO dbo.Rewards (RewardName, Description, IconCode, Category, CoinCost, IsSystem, IsEnabled)
    VALUES (N'Favorite Snack Pick', N'Choose a favorite snack for the week.', N'snack_pick', N'FoodTreat', 60, 1, 1);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Rewards
    WHERE RewardName = N'Park Visit Choice'
        AND FamilyId IS NULL
        AND IsSystem = 1
)
BEGIN
    INSERT INTO dbo.Rewards (RewardName, Description, IconCode, Category, CoinCost, IsSystem, IsEnabled)
    VALUES (N'Park Visit Choice', N'Choose the park for the next family outing.', N'park_visit', N'Outing', 140, 1, 1);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Rewards
    WHERE RewardName = N'Mini Outing Pick'
        AND FamilyId IS NULL
        AND IsSystem = 1
)
BEGIN
    INSERT INTO dbo.Rewards (RewardName, Description, IconCode, Category, CoinCost, IsSystem, IsEnabled)
    VALUES (N'Mini Outing Pick', N'Pick a short family outing destination.', N'mini_outing', N'Outing', 180, 1, 1);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Rewards
    WHERE RewardName = N'Small Toy Purchase'
        AND FamilyId IS NULL
        AND IsSystem = 1
)
BEGIN
    INSERT INTO dbo.Rewards (RewardName, Description, IconCode, Category, CoinCost, IsSystem, IsEnabled)
    VALUES (N'Small Toy Purchase', N'Choose a small toy or surprise item.', N'toy_purchase', N'Purchase', 220, 1, 1);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Rewards
    WHERE RewardName = N'Book of Choice'
        AND FamilyId IS NULL
        AND IsSystem = 1
)
BEGIN
    INSERT INTO dbo.Rewards (RewardName, Description, IconCode, Category, CoinCost, IsSystem, IsEnabled)
    VALUES (N'Book of Choice', N'Pick one new book to bring home.', N'book_choice', N'Purchase', 200, 1, 1);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Rewards
    WHERE RewardName = N'Choose Family Game Night'
        AND FamilyId IS NULL
        AND IsSystem = 1
)
BEGIN
    INSERT INTO dbo.Rewards (RewardName, Description, IconCode, Category, CoinCost, IsSystem, IsEnabled)
    VALUES (N'Choose Family Game Night', N'Choose the game for the next family game night.', N'game_night', N'FamilyActivity', 160, 1, 1);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Rewards
    WHERE RewardName = N'Pick Weekend Activity'
        AND FamilyId IS NULL
        AND IsSystem = 1
)
BEGIN
    INSERT INTO dbo.Rewards (RewardName, Description, IconCode, Category, CoinCost, IsSystem, IsEnabled)
    VALUES (N'Pick Weekend Activity', N'Pick the weekend family activity.', N'weekend_activity', N'FamilyActivity', 250, 1, 1);
END;
GO
