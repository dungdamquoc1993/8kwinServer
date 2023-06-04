-- --------------------------------------------------------
-- Host:                         10.40.96.4
-- Server version:               5.6.45 - MySQL Community Server (GPL)
-- Server OS:                    Linux
-- HeidiSQL Version:             10.2.0.5599
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;


-- Dumping database structure for cgame
CREATE DATABASE IF NOT EXISTS `cgame` /*!40100 DEFAULT CHARACTER SET latin1 */;
USE `cgame`;

-- Dumping structure for table cgame.admin
CREATE TABLE IF NOT EXISTS `admin` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `group_id` int(11) DEFAULT NULL,
  `img` varchar(255) COLLATE utf8_unicode_ci DEFAULT NULL COMMENT 'anh dai dien',
  `username` varchar(100) COLLATE utf8_unicode_ci DEFAULT NULL COMMENT 'ten dang nhap/email',
  `password` varchar(125) COLLATE utf8_unicode_ci DEFAULT NULL COMMENT 'mat khau',
  `fullname` varchar(150) COLLATE utf8_unicode_ci DEFAULT NULL COMMENT 'ten hien thi',
  `valid` tinyint(1) DEFAULT '1' COMMENT 'khoa/mo khoa',
  `no_edit` tinyint(1) DEFAULT '0' COMMENT 'duoc phep edit',
  `is_root` tinyint(1) DEFAULT '0' COMMENT 'tai khoan root',
  `cp` varchar(50) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `username` (`username`),
  KEY `group_id` (`group_id`),
  CONSTRAINT `admin_ibfk_2` FOREIGN KEY (`group_id`) REFERENCES `admin_group` (`id`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table cgame.admin_group
CREATE TABLE IF NOT EXISTS `admin_group` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) COLLATE utf8_unicode_ci DEFAULT NULL,
  `valid` tinyint(1) DEFAULT '1',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table cgame.admin_log
CREATE TABLE IF NOT EXISTS `admin_log` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `admin_id` int(11) DEFAULT NULL,
  `admin` varchar(50) COLLATE utf8_unicode_ci DEFAULT NULL,
  `action` varchar(50) COLLATE utf8_unicode_ci DEFAULT NULL,
  `category` varchar(100) COLLATE utf8_unicode_ci DEFAULT NULL COMMENT 'category permission',
  `table` varchar(50) COLLATE utf8_unicode_ci DEFAULT NULL COMMENT 'table focus',
  `row_id` int(11) DEFAULT NULL COMMENT 'id focus',
  `content` text COLLATE utf8_unicode_ci COMMENT 'du lieu raw',
  `content_new` text COLLATE utf8_unicode_ci COMMENT 'du lieu update',
  `note` text COLLATE utf8_unicode_ci,
  `create_time` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `admin_id` (`admin_id`),
  CONSTRAINT `admin_log_ibfk_1` FOREIGN KEY (`admin_id`) REFERENCES `admin` (`id`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table cgame.admin_menu
CREATE TABLE IF NOT EXISTS `admin_menu` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `menuhead_id` int(11) DEFAULT NULL,
  `menugroup_id` int(11) DEFAULT NULL,
  `category` varchar(50) COLLATE utf8_unicode_ci DEFAULT NULL,
  `name` varchar(200) COLLATE utf8_unicode_ci DEFAULT NULL,
  `icon` varchar(50) COLLATE utf8_unicode_ci DEFAULT 'fa fa-circle-o text-red' COMMENT 'fa fa-icon',
  `url` varchar(200) COLLATE utf8_unicode_ci DEFAULT NULL,
  `position` tinyint(1) DEFAULT '1',
  `valid` tinyint(1) DEFAULT '1',
  `is_group` int(11) DEFAULT NULL,
  `deleted` tinyint(1) DEFAULT '0' COMMENT 'khong co menu nay',
  PRIMARY KEY (`id`),
  KEY `parent_id` (`menugroup_id`),
  KEY `head_id` (`menuhead_id`),
  CONSTRAINT `admin_menu_ibfk_1` FOREIGN KEY (`menuhead_id`) REFERENCES `admin_menuhead` (`id`) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT `admin_menu_ibfk_2` FOREIGN KEY (`menugroup_id`) REFERENCES `admin_menu` (`id`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table cgame.admin_menuhead
CREATE TABLE IF NOT EXISTS `admin_menuhead` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(50) DEFAULT NULL,
  `position` tinyint(1) DEFAULT '1',
  `valid` tinyint(1) DEFAULT '1',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.admin_permission
CREATE TABLE IF NOT EXISTS `admin_permission` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(10) COLLATE utf8_unicode_ci DEFAULT NULL,
  `key` varchar(50) CHARACTER SET latin1 DEFAULT NULL,
  `category` varchar(50) CHARACTER SET latin1 DEFAULT NULL,
  `category_name` varchar(200) COLLATE utf8_unicode_ci DEFAULT NULL,
  `valid` tinyint(1) DEFAULT '1',
  PRIMARY KEY (`id`),
  UNIQUE KEY `key` (`key`,`category`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table cgame.admin_permission_group
CREATE TABLE IF NOT EXISTS `admin_permission_group` (
  `group_id` int(11) DEFAULT NULL,
  `permission_id` int(11) DEFAULT NULL,
  KEY `permission_id` (`permission_id`),
  KEY `group_id` (`group_id`),
  CONSTRAINT `admin_permission_group_ibfk_1` FOREIGN KEY (`group_id`) REFERENCES `admin_group` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `admin_permission_group_ibfk_2` FOREIGN KEY (`permission_id`) REFERENCES `admin_permission` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- Data exporting was unselected.

-- Dumping structure for table cgame.announces
CREATE TABLE IF NOT EXISTS `announces` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `title` varchar(200) DEFAULT NULL,
  `content` text,
  `type` int(11) DEFAULT '0' COMMENT '1: Thư hệ thống,\n0: Thư cá nhân',
  `priority` int(11) DEFAULT '0',
  `active` int(11) DEFAULT '1',
  `user_id` bigint(20) DEFAULT NULL,
  `sender` varchar(45) DEFAULT 'Hệ thống',
  `time_start` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `time_end` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `time_create` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `act_read` tinyint(1) DEFAULT '0',
  PRIMARY KEY (`id`),
  KEY `user_id_read` (`user_id`,`act_read`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.app_billing
CREATE TABLE IF NOT EXISTS `app_billing` (
  `id` int(11) NOT NULL,
  `user_id` bigint(20) unsigned NOT NULL,
  `product` varchar(50) NOT NULL,
  `amount` int(10) unsigned NOT NULL DEFAULT '0',
  `current_cash` bigint(20) unsigned NOT NULL DEFAULT '0',
  `telco` varchar(50) NOT NULL DEFAULT '',
  `order_id` varchar(50) NOT NULL DEFAULT '',
  `platform` varchar(50) NOT NULL DEFAULT '',
  `device_id` varchar(120) NOT NULL DEFAULT '',
  `create_time` datetime NOT NULL,
  `country` varchar(20) NOT NULL DEFAULT 'vi',
  `is_sum` tinyint(4) NOT NULL DEFAULT '0',
  `version` varchar(30) DEFAULT '',
  `ip_address` varchar(30) DEFAULT '',
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  KEY `create_time` (`create_time`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.bc_announce
CREATE TABLE IF NOT EXISTS `bc_announce` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` int(11) NOT NULL,
  `Time` datetime NOT NULL,
  `Content` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Type` tinyint(4) DEFAULT '0',
  `Stat` tinyint(4) DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `Time` (`Time`),
  KEY `UserId` (`UserId`),
  KEY `Stat` (`Stat`),
  KEY `Type` (`Type`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.bc_announce_all
CREATE TABLE IF NOT EXISTS `bc_announce_all` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `Time` datetime NOT NULL,
  `Content` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Type` tinyint(4) DEFAULT '0',
  `Stat` tinyint(4) DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `Time` (`Time`),
  KEY `Stat` (`Stat`),
  KEY `Type` (`Type`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.bc_ccu
CREATE TABLE IF NOT EXISTS `bc_ccu` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ServerId` smallint(6) NOT NULL,
  `Time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CCU` smallint(6) NOT NULL,
  `Profit` bigint(20) NOT NULL,
  `FreeSlot` smallint(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `Time` (`Time`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=latin1;

-- Data exporting was unselected.

-- Dumping structure for table cgame.bc_events
CREATE TABLE IF NOT EXISTS `bc_events` (
  `event_id` int(11) NOT NULL AUTO_INCREMENT,
  `title` varchar(100) DEFAULT NULL,
  `body` text,
  `is_new` int(11) DEFAULT '1',
  `is_show` int(11) DEFAULT '1',
  `event_type` int(11) DEFAULT '1',
  `url_icon` varchar(100) DEFAULT NULL,
  `url_image` varchar(100) DEFAULT NULL,
  `url_lobby` varchar(100) DEFAULT NULL,
  `top_event` varchar(100) DEFAULT NULL,
  `event_info` varchar(500) DEFAULT NULL,
  `join_event` varchar(45) DEFAULT NULL,
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `start_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `end_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `language` varchar(4) NOT NULL DEFAULT 'vi',
  `orderby` smallint(6) NOT NULL DEFAULT '0',
  `is_hot` tinyint(1) DEFAULT '1',
  PRIMARY KEY (`event_id`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.bc_events_detail
CREATE TABLE IF NOT EXISTS `bc_events_detail` (
  `lang_code` varchar(15) NOT NULL,
  `title` varchar(255) NOT NULL,
  `event_info` text NOT NULL,
  `event_id` int(11) NOT NULL,
  `join_event` varchar(45) DEFAULT NULL,
  `url_image` varchar(100) DEFAULT NULL,
  `url_lobby` varchar(100) DEFAULT NULL,
  UNIQUE KEY `lang_code` (`lang_code`,`event_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.bc_feedback
CREATE TABLE IF NOT EXISTS `bc_feedback` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` int(11) NOT NULL,
  `Time` datetime NOT NULL,
  `Content` text NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `Time` (`Time`),
  KEY `UserId` (`UserId`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.bc_fish_log
CREATE TABLE IF NOT EXISTS `bc_fish_log` (
  `Id` bigint(11) NOT NULL AUTO_INCREMENT,
  `TableId` int(11) NOT NULL,
  `TableBlind` tinyint(4) NOT NULL,
  `UserId` int(11) NOT NULL,
  `Cash` bigint(20) NOT NULL,
  `ChangeCash` int(11) NOT NULL,
  `FishType` tinyint(4) NOT NULL,
  `Time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ServerId` tinyint(4) NOT NULL,
  `Item` tinyint(4) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `TableId` (`TableId`),
  KEY `Time` (`Time`),
  KEY `UserId` (`UserId`),
  KEY `FishType` (`FishType`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- Data exporting was unselected.

-- Dumping structure for table cgame.bc_google_public_key
CREATE TABLE IF NOT EXISTS `bc_google_public_key` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `AppId` varchar(50) NOT NULL,
  `Key` text NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `AppId` (`AppId`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.bc_hacker
CREATE TABLE IF NOT EXISTS `bc_hacker` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` int(11) NOT NULL,
  `Time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Extra` text NOT NULL,
  `Type` tinyint(4) DEFAULT '0' COMMENT 'HackSpeed = 0,             InvalidBullet = 1,             PlaySlotWhilePlayBC = 2,             PlaySlotInvalidServer = 3,             PlayTxWhilePlayBC = 4,             PlayTxInvalidServer = 5,             CashOutWhilePlayBC = 6,             CashOutInvalidServer = 7,             ClientReport = 8,             CannotIncCash = 9,             TxInvalidParams = 10,',
  PRIMARY KEY (`Id`),
  KEY `Time` (`Time`),
  KEY `UserId` (`UserId`),
  KEY `Type` (`Type`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.bc_history
CREATE TABLE IF NOT EXISTS `bc_history` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` int(11) NOT NULL,
  `Time` datetime NOT NULL,
  `Cash` bigint(20) DEFAULT NULL,
  `TableId` int(11) NOT NULL,
  `TableBlind` tinyint(4) NOT NULL,
  `Type` tinyint(3) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `UserId` (`UserId`),
  KEY `Time` (`Time`),
  KEY `TableId` (`TableId`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.bc_iap
CREATE TABLE IF NOT EXISTS `bc_iap` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `AppId` varchar(50) NOT NULL,
  `VersionCodeMin` bigint(20) DEFAULT '0',
  `VersionCodeMax` bigint(20) DEFAULT '0',
  `Cash` bigint(20) NOT NULL,
  `Price` bigint(20) NOT NULL,
  `Title` varchar(50) DEFAULT NULL,
  `Description` varchar(200) DEFAULT NULL,
  `ProductId` varchar(50) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `AppId` (`AppId`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.bc_leaderboard_month
CREATE TABLE IF NOT EXISTS `bc_leaderboard_month` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` int(11) NOT NULL,
  `Username` varchar(50) NOT NULL,
  `Nickname` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Avatar` varchar(200) DEFAULT NULL,
  `Cash` bigint(20) NOT NULL,
  `Level` int(11) NOT NULL,
  `Rank` tinyint(4) NOT NULL,
  `CashGain` int(11) NOT NULL,
  `Time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Prize` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `Time` (`Time`),
  KEY `UserId` (`UserId`),
  KEY `Rank` (`Rank`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.bc_leaderboard_week
CREATE TABLE IF NOT EXISTS `bc_leaderboard_week` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` int(11) NOT NULL,
  `Username` varchar(50) NOT NULL,
  `Nickname` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Avatar` varchar(200) DEFAULT NULL,
  `Cash` bigint(20) NOT NULL,
  `Level` int(11) NOT NULL,
  `Rank` tinyint(4) NOT NULL,
  `CashGain` int(11) NOT NULL,
  `Time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Prize` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `Time` (`Time`),
  KEY `UserId` (`UserId`),
  KEY `Rank` (`Rank`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.bc_log
CREATE TABLE IF NOT EXISTS `bc_log` (
  `Id` bigint(11) NOT NULL AUTO_INCREMENT,
  `TableId` int(11) NOT NULL,
  `TableBlind` tinyint(4) NOT NULL,
  `UserId` int(11) NOT NULL,
  `Cash` bigint(20) NOT NULL,
  `ChangeCash` int(11) NOT NULL,
  `Reason` tinyint(4) NOT NULL COMMENT 'TableStart = 0,',
  `Time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ServerId` smallint(6) NOT NULL,
  `Item` tinyint(4) NOT NULL,
  `Extra` longtext COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `TableId` (`TableId`),
  KEY `Time` (`Time`),
  KEY `Reason` (`Reason`),
  KEY `UserId` (`UserId`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table cgame.bc_players
CREATE TABLE IF NOT EXISTS `bc_players` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `PlayerId` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Nickname` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Avatar` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT '0',
  `Cash` bigint(20) NOT NULL,
  `Exp` bigint(20) NOT NULL,
  `Level` int(11) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `Cash` (`Cash`),
  KEY `Level` (`Level`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table cgame.bc_slot5_glory
CREATE TABLE IF NOT EXISTS `bc_slot5_glory` (
  `trans_id` bigint(20) NOT NULL AUTO_INCREMENT,
  `user_id` bigint(20) NOT NULL,
  `nickname` varchar(45) DEFAULT NULL,
  `blind` int(11) DEFAULT NULL,
  `total_bet` bigint(20) DEFAULT '0',
  `win_cash` bigint(20) DEFAULT NULL,
  `win_type` int(11) DEFAULT '0',
  `bonus` varchar(45) DEFAULT NULL,
  `free_spin` tinyint(4) DEFAULT NULL,
  `description` varchar(45) DEFAULT NULL,
  `create_time` datetime DEFAULT NULL,
  PRIMARY KEY (`trans_id`),
  KEY `win_cash` (`win_cash`),
  KEY `create_time` (`create_time`),
  KEY `blind` (`blind`),
  KEY `user_id` (`user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.bc_slot5_histories
CREATE TABLE IF NOT EXISTS `bc_slot5_histories` (
  `trans_id` bigint(20) NOT NULL AUTO_INCREMENT,
  `user_id` bigint(20) NOT NULL,
  `nickname` varchar(45) DEFAULT NULL,
  `blind` int(11) DEFAULT NULL,
  `total_bet` bigint(20) DEFAULT '0',
  `win_lines` varchar(45) DEFAULT NULL,
  `slot` varchar(45) DEFAULT NULL,
  `request_lines` varchar(100) DEFAULT NULL,
  `win_cash` bigint(20) DEFAULT NULL,
  `win_type` int(11) DEFAULT '0',
  `bonus` varchar(45) DEFAULT NULL,
  `free_spin` tinyint(4) DEFAULT NULL,
  `description` varchar(45) DEFAULT NULL,
  `create_time` datetime DEFAULT NULL,
  `is_sum` tinyint(4) DEFAULT '0',
  PRIMARY KEY (`trans_id`),
  KEY `win_cash` (`win_cash`),
  KEY `create_time` (`create_time`),
  KEY `blind` (`blind`),
  KEY `blind_create_time` (`blind`,`create_time`),
  KEY `user_id` (`user_id`),
  KEY `is_sum` (`is_sum`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.bc_trans_log
CREATE TABLE IF NOT EXISTS `bc_trans_log` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` int(11) NOT NULL,
  `Cash` bigint(20) NOT NULL,
  `CashGain` int(11) NOT NULL,
  `Time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Extra` text NOT NULL,
  `Type` tinyint(4) DEFAULT '0',
  PRIMARY KEY (`Id`),
  KEY `Time` (`Time`),
  KEY `UserId` (`UserId`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.big_small_glory
CREATE TABLE IF NOT EXISTS `big_small_glory` (
  `trans_id` bigint(20) NOT NULL AUTO_INCREMENT,
  `user_id` bigint(20) DEFAULT NULL,
  `nickname` varchar(45) DEFAULT NULL,
  `total_bet` bigint(20) DEFAULT '0',
  `win_cash` bigint(20) DEFAULT NULL,
  `game_session` bigint(20) DEFAULT NULL,
  `description` varchar(500) DEFAULT 'Thắng lớn',
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`trans_id`),
  KEY `create_time` (`create_time`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.big_small_histoies
CREATE TABLE IF NOT EXISTS `big_small_histoies` (
  `trans_id` bigint(20) unsigned NOT NULL,
  `dices` varchar(45) DEFAULT NULL,
  `small_money` bigint(20) DEFAULT NULL,
  `big_money` bigint(20) DEFAULT NULL,
  `small_count` int(11) DEFAULT NULL,
  `big_count` int(11) DEFAULT NULL,
  `bot_small_bet` bigint(20) DEFAULT '0',
  `bot_big_bet` bigint(20) DEFAULT '0',
  `big_balance` bigint(20) DEFAULT '0',
  `small_balance` bigint(20) DEFAULT '0',
  `current_bank` bigint(20) DEFAULT '0',
  `bot_win_lose` bigint(20) DEFAULT '0',
  `fee` bigint(20) DEFAULT '0',
  `current_fee` bigint(20) DEFAULT '0',
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`trans_id`),
  KEY `create_time` (`create_time`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.big_small_transactions
CREATE TABLE IF NOT EXISTS `big_small_transactions` (
  `trans_id` int(11) NOT NULL AUTO_INCREMENT,
  `user_id` bigint(20) DEFAULT NULL,
  `nickname` varchar(45) DEFAULT NULL,
  `game_session` bigint(20) DEFAULT NULL,
  `big_bet` bigint(20) DEFAULT NULL,
  `small_bet` bigint(20) DEFAULT NULL,
  `win_cash` bigint(20) DEFAULT NULL,
  `big_refund` bigint(20) DEFAULT NULL,
  `small_refund` bigint(20) DEFAULT NULL,
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `is_sum` tinyint(3) DEFAULT '0',
  PRIMARY KEY (`trans_id`),
  KEY `gameSession` (`game_session`),
  KEY `create_time` (`create_time`),
  KEY `user_id` (`user_id`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.card
CREATE TABLE IF NOT EXISTS `card` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `name` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `type` varchar(10) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `amount` int(4) DEFAULT NULL,
  `pin` varchar(125) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `serial` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `status` tinyint(1) DEFAULT '0' COMMENT '0:normal, 1:used',
  `code` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_time` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `created_by` varchar(25) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `user_id` int(11) DEFAULT NULL,
  `updated_time` datetime DEFAULT NULL,
  `updated_by` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `pin` (`pin`,`serial`,`type`)
) ENGINE=MyISAM AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table cgame.card_log
CREATE TABLE IF NOT EXISTS `card_log` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `card_code` varchar(25) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `content` text COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table cgame.cashouts
CREATE TABLE IF NOT EXISTS `cashouts` (
  `trans_id` int(11) NOT NULL AUTO_INCREMENT,
  `user_id` bigint(20) DEFAULT '0',
  `item_id` varchar(45) DEFAULT NULL,
  `seri` varchar(20) DEFAULT NULL,
  `number_card` varchar(20) DEFAULT NULL,
  `price` int(10) unsigned DEFAULT NULL,
  `username` varchar(45) DEFAULT NULL,
  `telco` varchar(45) DEFAULT NULL,
  `platform` varchar(20) DEFAULT 'ios',
  `device_id` varchar(45) DEFAULT NULL,
  `status` int(11) DEFAULT '0' COMMENT '0: Chờ duyệt\n1 : đã duyệt\n2 : hủy\n3: Đang xử lý',
  `time_cashout` datetime DEFAULT NULL,
  `time_approval` datetime DEFAULT NULL,
  `is_sum` tinyint(3) DEFAULT '0',
  `ip_address` varchar(30) DEFAULT '' COMMENT 'ip nguoi dung',
  `admin_approval` tinyint(3) DEFAULT '0',
  `to_money` float DEFAULT '0',
  `paypal_email` varchar(120) DEFAULT '',
  `paypal_tran_id` varchar(50) DEFAULT '',
  `paypal_phone` varchar(20) DEFAULT '',
  `paypal_fullname` varchar(100) DEFAULT '',
  `response` text,
  `cp` varchar(50) DEFAULT NULL,
  `updated_by` varchar(25) DEFAULT NULL,
  PRIMARY KEY (`trans_id`),
  KEY `user_id` (`user_id`),
  KEY `time_cashout` (`time_cashout`),
  KEY `time_approval` (`time_approval`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for event cgame.clean_bc_log
DELIMITER //
CREATE DEFINER=`root`@`localhost` EVENT `clean_bc_log` ON SCHEDULE EVERY 4 HOUR STARTS '2019-10-15 07:10:58' ON COMPLETION NOT PRESERVE ENABLE DO DELETE FROM `bc_log` WHERE Time < NOW() - INTERVAL 30 DAY//
DELIMITER ;

-- Dumping structure for table cgame.daily
CREATE TABLE IF NOT EXISTS `daily` (
  `date` date NOT NULL,
  `total_user_active` int(11) DEFAULT '0',
  `total_user_active_android` int(11) DEFAULT '0',
  `total_user_active_ios` int(11) DEFAULT '0',
  `total_user_new` int(11) DEFAULT '0' COMMENT 'new user reg',
  `total_device_new` int(11) DEFAULT '0',
  `total_device_new_android` int(11) DEFAULT NULL,
  `total_device_new_ios` int(11) DEFAULT '0',
  `total_paying` int(11) DEFAULT '0',
  `total_paying_new` int(11) DEFAULT '0',
  `total_paying_new_android` int(11) DEFAULT '0',
  `total_paying_new_ios` int(11) DEFAULT '0',
  `total_cash_in` int(11) DEFAULT '0',
  `total_cash_in_android` int(11) DEFAULT '0',
  `total_cash_in_ios` int(11) DEFAULT '0',
  `total_cash_in_card` int(11) DEFAULT '0',
  `total_cash_in_iap` int(11) DEFAULT '0',
  `total_cash_out` int(11) DEFAULT '0',
  `updated_time` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY `date` (`date`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table cgame.daily_cp
CREATE TABLE IF NOT EXISTS `daily_cp` (
  `cp` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `date` date NOT NULL,
  `total_user_active` int(11) DEFAULT '0',
  `total_user_new` int(11) DEFAULT '0' COMMENT 'new user reg',
  `total_device_new` int(11) DEFAULT '0',
  `total_paying` int(11) DEFAULT '0',
  `total_paying_new` int(11) DEFAULT '0',
  `total_cash_in` int(11) DEFAULT '0',
  `total_cash_in_android` int(11) DEFAULT '0',
  `total_cash_in_ios` int(11) DEFAULT '0',
  `total_cash_in_card` int(11) DEFAULT '0',
  `total_cash_in_iap` int(11) DEFAULT '0',
  `total_cash_out` int(11) DEFAULT '0',
  UNIQUE KEY `date` (`date`,`cp`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table cgame.daily_game
CREATE TABLE IF NOT EXISTS `daily_game` (
  `date` date DEFAULT NULL,
  `fish_cash_in` int(11) DEFAULT '0',
  `fish_cash_out` int(11) DEFAULT '0',
  `solo_cash_in` int(11) DEFAULT '0',
  `solo_cash_out` int(11) DEFAULT '0',
  `slot_cash_in` int(11) DEFAULT '0',
  `slot_cash_out` int(11) DEFAULT '0',
  `taixiu_cash_in` int(11) DEFAULT '0',
  `taixiu_cash_out` int(11) DEFAULT '0',
  `updated_time` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY `date` (`date`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table cgame.giftcode_campaign
CREATE TABLE IF NOT EXISTS `giftcode_campaign` (
  `campaign_id` bigint(20) NOT NULL AUTO_INCREMENT,
  `campaign_name` varchar(50) DEFAULT NULL,
  `cash` bigint(20) DEFAULT NULL,
  `quantity` int(11) DEFAULT NULL,
  `status` tinyint(4) DEFAULT NULL,
  `create_time` datetime DEFAULT NULL,
  PRIMARY KEY (`campaign_id`),
  KEY `create_time` (`create_time`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.gift_codes
CREATE TABLE IF NOT EXISTS `gift_codes` (
  `gift_code` varchar(20) NOT NULL,
  `campaign_id` int(11) DEFAULT NULL,
  `cash` bigint(20) DEFAULT NULL,
  `status` tinyint(4) DEFAULT '0',
  `receiver_id` bigint(20) DEFAULT NULL,
  `phone_number` varchar(45) DEFAULT NULL,
  `receiver` varchar(45) DEFAULT NULL,
  `use_time` datetime DEFAULT NULL,
  `device_id` varchar(120) DEFAULT '',
  `facebook_id` varchar(150) DEFAULT NULL,
  PRIMARY KEY (`gift_code`),
  KEY `receiver` (`receiver`),
  KEY `use_time` (`use_time`),
  KEY `device_id` (`device_id`),
  KEY `phone_number` (`phone_number`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.iap_logs
CREATE TABLE IF NOT EXISTS `iap_logs` (
  `id` bigint(20) unsigned NOT NULL,
  `user_id` int(11) DEFAULT '0',
  `signature` text,
  `signeddata` text,
  `time_created` datetime DEFAULT NULL,
  `platform` varchar(100) DEFAULT NULL,
  `version` varchar(20) DEFAULT '',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.login_start
CREATE TABLE IF NOT EXISTS `login_start` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `user_id` bigint(20) DEFAULT '0',
  `device_id` varchar(50) DEFAULT '',
  `platform` varchar(45) DEFAULT NULL,
  `ip` varchar(45) DEFAULT NULL,
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `logout_time` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.minigame
CREATE TABLE IF NOT EXISTS `minigame` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `device_id` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `device_name` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `os_version` varchar(25) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `platform` enum('android','ios') COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `ip` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `updated_time` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table cgame.partner
CREATE TABLE IF NOT EXISTS `partner` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `code` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `valid` tinyint(1) DEFAULT '1',
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table cgame.request_card
CREATE TABLE IF NOT EXISTS `request_card` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `trans_id` varchar(50) DEFAULT NULL,
  `app_id` varchar(50) DEFAULT NULL,
  `app_trans_id` varchar(50) DEFAULT NULL COMMENT 'app trans id',
  `app_user_id` int(11) DEFAULT NULL,
  `app_username` varchar(50) DEFAULT NULL,
  `app_status` tinyint(1) DEFAULT '0' COMMENT '0:no_response 1:success -1:failed -2:supect',
  `card_type` varchar(10) DEFAULT NULL,
  `card_serial` varchar(45) DEFAULT NULL,
  `card_pin` varchar(45) DEFAULT NULL,
  `card_amount` double DEFAULT '0',
  `gold` double DEFAULT '0',
  `gw_amount` double DEFAULT NULL,
  `gw_status` int(11) DEFAULT NULL,
  `gw_message` text,
  `note` varchar(512) DEFAULT NULL,
  `created_time` datetime DEFAULT NULL,
  `process_status` tinyint(1) DEFAULT '100',
  `process_time` datetime DEFAULT NULL,
  `provider` varchar(25) DEFAULT NULL,
  `partner` varchar(25) DEFAULT NULL,
  `platform` tinyint(1) DEFAULT NULL,
  `suspect` tinyint(1) DEFAULT '0',
  PRIMARY KEY (`id`),
  KEY `USER_INDX` (`app_user_id`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.slot5_glory
CREATE TABLE IF NOT EXISTS `slot5_glory` (
  `trans_id` bigint(20) NOT NULL AUTO_INCREMENT,
  `user_id` bigint(20) NOT NULL,
  `nickname` varchar(45) DEFAULT NULL,
  `blind` int(11) DEFAULT NULL,
  `total_bet` bigint(20) DEFAULT '0',
  `win_cash` bigint(20) DEFAULT NULL,
  `win_type` int(11) DEFAULT '0',
  `bonus` varchar(45) DEFAULT NULL,
  `free_spin` tinyint(4) DEFAULT NULL,
  `description` varchar(45) DEFAULT NULL,
  `create_time` datetime DEFAULT NULL,
  PRIMARY KEY (`trans_id`),
  KEY `win_cash` (`win_cash`),
  KEY `create_time` (`create_time`),
  KEY `blind` (`blind`),
  KEY `user_id` (`user_id`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for table cgame.slot5_histories
CREATE TABLE IF NOT EXISTS `slot5_histories` (
  `trans_id` bigint(20) NOT NULL AUTO_INCREMENT,
  `user_id` bigint(20) NOT NULL,
  `nickname` varchar(45) DEFAULT NULL,
  `blind` int(11) DEFAULT NULL,
  `total_bet` bigint(20) DEFAULT '0',
  `win_lines` varchar(100) DEFAULT NULL,
  `slot` varchar(100) DEFAULT NULL,
  `request_lines` varchar(128) DEFAULT NULL,
  `win_cash` bigint(20) DEFAULT NULL,
  `win_type` int(11) DEFAULT '0',
  `bonus` varchar(45) DEFAULT NULL,
  `free_spin` tinyint(4) DEFAULT NULL,
  `description` varchar(45) DEFAULT NULL,
  `create_time` datetime DEFAULT NULL,
  `is_sum` tinyint(4) DEFAULT '0',
  PRIMARY KEY (`trans_id`),
  KEY `win_cash` (`win_cash`),
  KEY `create_time` (`create_time`),
  KEY `blind` (`blind`),
  KEY `blind_create_time` (`blind`,`create_time`),
  KEY `user_id` (`user_id`),
  KEY `is_sum` (`is_sum`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping structure for procedure cgame.SP_CashoutHistories
DELIMITER //
CREATE DEFINER=`root`@`localhost` PROCEDURE `SP_CashoutHistories`(IN `_username` VARCHAR(45), IN `_status` TINYINT, IN `_trans_id` TINYINT)
BEGIN
	SELECT * FROM cashouts WHERE 
		IF(_username is null or _username ='', true , username = _username)
        AND IF(_status =-1, true, status = _status)
        AND IF(_trans_id =0, true, trans_id = _trans_id)
        ORDER BY time_cashout DESC
        LIMIT 50;
END//
DELIMITER ;

-- Dumping structure for procedure cgame.SP_GIFT_CODE
DELIMITER //
CREATE DEFINER=`root`@`localhost` PROCEDURE `SP_GIFT_CODE`(IN `_gift_code` VARCHAR(20), IN `_user_receiver` VARCHAR(50), IN `_phone_number` VARCHAR(50))
This:BEGIN
	DECLARE _status, _amount, _compaign, _count INT;
    SELECT status, cash, campaign_id INTO _status, _amount, _compaign
		FROM `gift_codes` 
        WHERE gift_code = _gift_code LIMIT 1;
    IF _status IS NULL OR _status = 1 THEN 
		SELECT 0 amount, 1 `error`; 
	ELSEIF _status IS NULL OR _status =3 THEN 
		SELECT 0 amount, 3 `error`; 
	ELSE
		SELECT count(*) INTO _count FROM `gift_codes` 
			WHERE campaign_id = _compaign AND phone_number = _phone_number LIMIT 1;
		IF _count > 0 THEN
			SELECT 0 amount, 2 `error`;
		ELSE
			UPDATE `gift_codes`
				SET
				`status` = 1,
				`receiver` = _user_receiver,
                `phone_number` = _phone_number,
				`use_time` = now()
			WHERE `gift_code` = _gift_code;
		SELECT _amount amount, 0 `error`; 
    END IF;
    END IF;
END//
DELIMITER ;

-- Dumping structure for procedure cgame.SP_Login
DELIMITER //
CREATE DEFINER=`root`@`localhost` PROCEDURE `SP_Login`(
	IN `_username` VARCHAR(50),
	IN `_password` VARCHAR(45)
)
BEGIN
	DECLARE _uname varchar(50) DEFAULT NULL;
    DECLARE _count INT DEFAULT 0;
    DECLARE _active INT DEFAULT 0;
    
    SELECT username, active INTO _uname, _active FROM users 
		WHERE username = _username LIMIT 1;
        
    IF _uname IS NULL THEN
        SELECT 1 error, 'This account is not exists' msg;
    ELSE 
		IF _active = 0 THEN
			SELECT 2 error, 'This account is ban' msg;
		ELSE
            SELECT u.user_id, u.nickname,u.device_id,u.platform, u.cash, u.cash_silver, u.cash_safe, u.phone_number, u.vip_id, u.avatar,u.language,u.publicprofile, u.vip_point,u.`block`,u.`time_login`,u.total_friend,u.gender,u.age,u.marries,u.level,u.like,u.game,u.description,u.trust,u.url_facebook,u.url_twitter, u.time_register, u.cp, u.verify_login, 0 error, 'success' msg 
                FROM users u 
				WHERE username = _username AND password = _password LIMIT 1;
		END IF;
	END IF;
END//
DELIMITER ;

-- Dumping structure for procedure cgame.SP_LoginByDevice
DELIMITER //
CREATE DEFINER=`root`@`localhost` PROCEDURE `SP_LoginByDevice`(IN `_username` VARCHAR(50), IN `_password` VARCHAR(45), IN `_platform` VARCHAR(20), IN `_device_id` VARCHAR(50), IN `_ip` VARCHAR(20), IN `_language` VARCHAR(20))
BEGIN
	DECLARE _uname varchar(50) DEFAULT NULL;
    DECLARE _count INT DEFAULT 0;
    DECLARE _active INT DEFAULT 0;
    
    SELECT username, active INTO _uname, _active FROM users 
		WHERE device_id = _device_id and type = 2 LIMIT 1;
        
    IF _uname IS NULL THEN
        INSERT INTO users (username, password, platform, device_id, type, ip,language, time_register,avatar)
		VALUES (_username, _password, _platform, _device_id, 2, _ip,_language, now(),ROUND( 100.0 * RAND( ) ));
        SELECT 0 error, 'Success' msg, _username as `username`;
    ELSE 
		IF _active = 0 THEN
			SELECT 2 error, 'This account is ban' msg;
		ELSE
			SELECT count(*) INTO _count FROM users 
				WHERE username = _uname AND password = _password LIMIT 1;
			IF _count > 0 THEN
				SELECT 0 error, 'success' msg, _uname as `username`;
			ELSE 
				SELECT 3 error, 'Incorrect password' msg;
			END IF;
		END IF;
	END IF;
END//
DELIMITER ;

-- Dumping structure for procedure cgame.SP_LoginFacebook
DELIMITER //
CREATE DEFINER=`root`@`localhost` PROCEDURE `SP_LoginFacebook`(IN `_facebook_id` VARCHAR(20), IN `_username` VARCHAR(50), IN `_password` VARCHAR(45), IN `_nickname` VARCHAR(50), IN `_platform` VARCHAR(20), IN `_avatar` VARCHAR(200), IN `_device_id` VARCHAR(50), IN `_ip` VARCHAR(20), IN `_language` VARCHAR(20))
BEGIN
	DECLARE _uname varchar(50) DEFAULT NULL;
    DECLARE _count INT DEFAULT 0;
    DECLARE _active INT DEFAULT 0;
    
    SELECT username, active INTO _uname, _active FROM users 
		WHERE facebook_id = _facebook_id LIMIT 1;
        
    IF _uname IS NULL THEN
        INSERT INTO users (username, nickname, password, platform, facebook_id,
							device_id, avatar, type, ip,language, time_register)
		VALUES (_username, _nickname, _password, _platform, _facebook_id,
							_device_id, _avatar, 1, _ip,_language, now());
        SELECT 0 error, 'Success' msg, _username as `username`;
    ELSE 
		IF _active = 0 THEN
			SELECT 2 error, 'This account is ban' msg;
		ELSE
			SELECT count(*) INTO _count FROM users 
				WHERE username = _uname AND password = _password;
			IF _count > 0 THEN
				SELECT 0 error, 'success' msg, _uname as `username`;
			ELSE 
				SELECT 3 error, 'Incorrect password' msg;
			END IF;
		END IF;
	END IF;
END//
DELIMITER ;

-- Dumping structure for procedure cgame.SP_Register
DELIMITER //
CREATE DEFINER=`root`@`localhost` PROCEDURE `SP_Register`(IN `_username` VARCHAR(50), IN `_password` VARCHAR(45), IN `_platform` VARCHAR(20), IN `_avatar` VARCHAR(20), IN `_device_id` VARCHAR(50), IN `_ip` VARCHAR(20), IN `_language` VARCHAR(20))
BEGIN
	DECLARE _uname varchar(50) DEFAULT NULL;
    DECLARE _uid bigint(50) DEFAULT NULL;
    SELECT username INTO _uname FROM users WHERE username = _username LIMIT 1;
    IF _uname IS NULL THEN
		INSERT INTO users (username, password, platform, avatar, device_id, ip,language, time_register)
		VALUES (_username, _password, _platform, _avatar, _device_id, _ip,_language , now());
        SELECT 0 error, 'Success' msg;
    ELSE 
		SELECT 1 error, 'This account is exists' msg;
	END IF;
END//
DELIMITER ;

-- Dumping structure for table cgame.transfer_history
CREATE TABLE IF NOT EXISTS `transfer_history` (
  `Id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `FromUserId` bigint(20) unsigned NOT NULL DEFAULT '0',
  `ToUserId` bigint(20) unsigned NOT NULL DEFAULT '0',
  `FromCash` bigint(20) unsigned NOT NULL DEFAULT '0',
  `ToCash` bigint(20) unsigned NOT NULL DEFAULT '0',
  `Time` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `FromUserId` (`FromUserId`),
  KEY `ToUserId` (`ToUserId`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=latin1;

-- Data exporting was unselected.

-- Dumping structure for table cgame.users
CREATE TABLE IF NOT EXISTS `users` (
  `user_id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `publicprofile` tinyint(4) DEFAULT '1',
  `username` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `nickname` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `password` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `active` int(11) DEFAULT '1',
  `type` int(11) DEFAULT '0' COMMENT '0 : normal user\n1: facebook user \n2: try user',
  `cash` bigint(20) unsigned DEFAULT '0',
  `cash_safe` bigint(20) unsigned DEFAULT '0',
  `cash_silver` bigint(20) DEFAULT '0',
  `vip_point` int(11) DEFAULT '0',
  `phone_number` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `vip_id` tinyint(4) DEFAULT '0',
  `platform` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `facebook_id` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `twitter_id` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT '',
  `device_id` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `avatar` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT '0',
  `ip` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT '127.0.0.1',
  `time_register` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `time_login` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `block` tinyint(3) DEFAULT '0',
  `language` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT 'en',
  `trust` int(10) unsigned DEFAULT '0',
  `total_friend` int(11) DEFAULT '0',
  `gender` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT '',
  `age` int(11) DEFAULT '0',
  `marries` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT '',
  `level` int(11) DEFAULT '0',
  `like` int(11) DEFAULT '0',
  `game` mediumtext COLLATE utf8mb4_unicode_ci,
  `description` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT '',
  `url_twitter` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT '',
  `url_facebook` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT '',
  `phone_contact` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT '',
  `appId` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `vcode` int(10) NOT NULL DEFAULT '0',
  `card_failed_daily` tinyint(1) DEFAULT '0',
  `card_failed_latest` tinyint(1) DEFAULT '0',
  `card_failed_total` tinyint(1) DEFAULT '0',
  `cp` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `verify_login` tinyint(3) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`user_id`),
  KEY `AppId` (`appId`),
  KEY `DeviceId` (`device_id`),
  KEY `Username` (`username`) USING BTREE,
  KEY `FbId` (`facebook_id`) USING BTREE,
  KEY `Nickname` (`nickname`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
