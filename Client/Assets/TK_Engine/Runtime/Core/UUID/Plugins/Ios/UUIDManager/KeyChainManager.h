//
//  KeychainManager.h
//  Unity-iPhone
//
//  Created by fotoable on 2020/6/29.
//

#import <Foundation/Foundation.h>

#import <Foundation/Foundation.h>

@interface KeyChainManager : NSObject

+ (NSMutableDictionary *)getKeychainQuery:(NSString *)service;

+ (void)save:(NSString *)service data:(id)data;

+ (id)load:(NSString *)service;

+ (void)delete:(NSString *)service;

@end
