CREATE TABLE `chatgptrecords` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `TheDay` int NOT NULL,
  `MsgId` varchar(255) NOT NULL,
  `UserId` varchar(255) NOT NULL,
  `Question` text NOT NULL,
  `Answer` text,
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP,
  `UpdateTime` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `UniqueKey` varchar(45) DEFAULT NULL,
  PRIMARY KEY (`ID`),
  KEY `day_user` (`TheDay`,`UserId`)
) ENGINE=InnoDB AUTO_INCREMENT=288 DEFAULT CHARSET=utf8;