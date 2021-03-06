﻿/*!40101 SET NAMES utf8 */;
/*!40101 SET SQL_MODE=''*/;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE DATABASE /*!32312 IF NOT EXISTS*/`Weave` /*!40100 DEFAULT CHARACTER SET latin1 */;

USE `Weave`;

CREATE TABLE `Users` (
  `UserId` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserName` VARCHAR(32) NOT NULL,
  `Md5` VARCHAR(128) NOT NULL,
  `Email` VARCHAR(64) DEFAULT NULL,
  PRIMARY KEY (`UserId`)
) ENGINE=INNODB DEFAULT CHARSET=latin1;

CREATE TABLE `Wbos` (
  `UserId` bigint(20) NOT NULL,
  `Collection` SMALLINT(6) NOT NULL,
  `Id` VARCHAR(64) NOT NULL,
  `Modified` DOUBLE DEFAULT NULL,
  `SortIndex` bigint(20) DEFAULT NULL,
  `Payload` TEXT,
  `PayloadSize` bigint(20) DEFAULT NULL,
  `Ttl` DOUBLE DEFAULT NULL,
  PRIMARY KEY (`UserId`,`Collection`,`Id`),
  KEY `Index_UserId_Collection_Modified` (`UserId`,`Collection`,`Modified`),
  KEY `Index_Ttl` (`Ttl`)
) ENGINE=INNODB DEFAULT CHARSET=latin1;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;