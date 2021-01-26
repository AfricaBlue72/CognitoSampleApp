#!/bin/sh

#./deploy.sh 
REGION='eu-west-1'

delete=false
deploy=true
dryrun=false
verbose=false

run() {
	${verbose} && echo "${@}"
	${dryrun} || "${@}"
}

while getopts "d" arg
do
        case "${arg}" in
        d)
                delete=true
                deploy=false
                ;;
	esac
done


#Start the deployment
echo "##########################################################################"

if(${deploy})
then
    echo 'Deploying Managed AD'
    cfn-create-or-update --stack-name test-congito --template-body file://test-cognito.yml \
    --parameters \
    ParameterKey=UserPoolName,ParameterValue=TestUserPool \
    ParameterKey=IdentityPoolName,ParameterValue=TestIdentityPool \
    --capabilities CAPABILITY_NAMED_IAM CAPABILITY_AUTO_EXPAND \
    --region ${REGION} --wait
fi

if(${delete})
then
    echo 'Delete Cognito'
    aws cloudformation delete-stack --stack-name test-congito || continue 
    aws cloudformation wait stack-delete-complete --stack-name test-cognito
fi
 