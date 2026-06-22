//
//  ATMintegralConfigure.h
//  AnyThinkMintegralAdapter
//
//  Created by Ben on 2022/6/13.
//  Copyright © 2022 AnyThink. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <AnyThinkSDK/ATNetwokBaseConfigure.h>

NS_ASSUME_NONNULL_BEGIN

@interface ATMintegralConfigure : ATNetwokBaseConfigure

@property (nonatomic,strong,readonly) NSString *appid;
@property (nonatomic,strong,readonly) NSString *appKey;


/// init mintegral
/// @param appid mintegral appid
/// @param appkey mintegral appkey
- (instancetype)initWithAppid:(NSString *)appid appkey:(NSString *)appkey;

@end

NS_ASSUME_NONNULL_END
